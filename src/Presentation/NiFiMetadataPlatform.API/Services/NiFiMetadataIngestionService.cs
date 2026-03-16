using System.Text.Json;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Entities;
using NiFiMetadataPlatform.Domain.Enums;
using NiFiMetadataPlatform.Domain.ValueObjects;
using NiFiMetadataPlatform.API.Models;

namespace NiFiMetadataPlatform.API.Services;

/// <summary>
/// Service for ingesting NiFi metadata from running containers.
/// </summary>
public sealed class NiFiMetadataIngestionService : INiFiMetadataIngestionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISearchRepository _searchRepository;
    private readonly IGraphRepository _graphRepository;
    private readonly IDockerContainerService _containerService;
    private readonly IGSPSqlParserService _gspParser;
    private readonly INiFiSchemaExtractor _schemaExtractor;
    private readonly IColumnLineageMapper _columnMapper;
    private readonly ILogger<NiFiMetadataIngestionService> _logger;

    // Cache for processor columns to avoid re-fetching
    private readonly Dictionary<string, List<SchemaColumn>> _processorColumnsCache = new();
    
    // Cache for processor table names (for PutDatabaseRecord processors)
    private readonly Dictionary<string, (string TableName, string? CatalogName, string? SchemaName, string Vendor, string? DbcpService)> _processorTableNameCache = new();
    
    // Cache for GSP lineage results (for creating table-to-processor edges)
    private readonly Dictionary<string, (GSPLineageResult Result, string Vendor, string? DbcpService)> _gspLineageCache = new();

    public NiFiMetadataIngestionService(
        IHttpClientFactory httpClientFactory,
        ISearchRepository searchRepository,
        IGraphRepository graphRepository,
        IDockerContainerService containerService,
        IGSPSqlParserService gspParser,
        INiFiSchemaExtractor schemaExtractor,
        IColumnLineageMapper columnMapper,
        ILogger<NiFiMetadataIngestionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _searchRepository = searchRepository;
        _graphRepository = graphRepository;
        _containerService = containerService;
        _gspParser = gspParser;
        _schemaExtractor = schemaExtractor;
        _columnMapper = columnMapper;
        _logger = logger;
    }

    public async Task<int> IngestFromContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting NiFi metadata ingestion for container {ContainerId}", containerId);

        // Get container info to find the NiFi port
        var containerInfo = await _containerService.GetContainerAsync(containerId);
        if (containerInfo == null)
        {
            throw new InvalidOperationException($"Container {containerId} not found");
        }

        // Find the NiFi port mapping (8080)
        var nifiPort = FindNiFiPort(containerInfo);
        if (nifiPort == null)
        {
            throw new InvalidOperationException($"NiFi port not found for container {containerId}");
        }

        _logger.LogInformation("Found NiFi at port {Port}", nifiPort);

        // Create HTTP client for NiFi
        // Use container name instead of localhost since we're inside Docker network
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri($"http://{containerInfo.Name}:8080");
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var entitiesIngested = 0;

        // Get root process group
        var rootResponse = await httpClient.GetAsync("/nifi-api/flow/process-groups/root", cancellationToken);
        if (!rootResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to access NiFi API: {rootResponse.StatusCode}");
        }

        var rootContent = await rootResponse.Content.ReadAsStringAsync(cancellationToken);
        var rootDoc = JsonDocument.Parse(rootContent);
        var processGroupFlow = rootDoc.RootElement.GetProperty("processGroupFlow");
        var flow = processGroupFlow.GetProperty("flow");

        // Get root process group name
        var rootPgName = processGroupFlow.TryGetProperty("breadcrumb", out var rootBreadcrumb) &&
                         rootBreadcrumb.TryGetProperty("breadcrumb", out var rootInnerBreadcrumb) &&
                         rootInnerBreadcrumb.TryGetProperty("name", out var rootNameElement)
            ? rootNameElement.GetString()
            : "NiFi Flow";

        // Get root process group ID
        var rootPgId = processGroupFlow.GetProperty("id").GetString();

        // Build root ancestor chain (just the root process group)
        var rootAncestorChain = new List<(string id, string name)>();
        if (!string.IsNullOrWhiteSpace(rootPgId) && !string.IsNullOrWhiteSpace(rootPgName))
        {
            rootAncestorChain.Add((rootPgId, rootPgName));
        }

        // Process processors in root
        if (flow.TryGetProperty("processors", out var processors))
        {
            foreach (var processorElement in processors.EnumerateArray())
            {
                try
                {
                    await IngestProcessorAsync(processorElement, containerId, rootPgId ?? "root", rootAncestorChain, httpClient, cancellationToken);
                    entitiesIngested++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest processor");
                }
            }
        }

        // Process connections for lineage
        if (flow.TryGetProperty("connections", out var connections))
        {
            foreach (var connectionElement in connections.EnumerateArray())
            {
                try
                {
                    await IngestConnectionAsync(connectionElement, containerId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest connection");
                }
            }
        }

        // Process child process groups recursively
        if (flow.TryGetProperty("processGroups", out var processGroups))
        {
            foreach (var processGroupElement in processGroups.EnumerateArray())
            {
                try
                {
                    var pgComponent = processGroupElement.GetProperty("component");
                    var pgId = pgComponent.GetProperty("id").GetString();
                    
                    if (pgId != null)
                    {
                        // Pass root ancestor chain to child process groups
                        entitiesIngested += await IngestProcessGroupAsync(httpClient, containerId, pgId, rootAncestorChain, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest process group");
                }
            }
        }

        _logger.LogInformation("Completed NiFi metadata ingestion. Total entities: {Count}", entitiesIngested);
        return entitiesIngested;
    }

    private async Task<int> IngestProcessGroupAsync(
        HttpClient httpClient,
        string containerId,
        string processGroupId,
        List<(string id, string name)> parentChain,
        CancellationToken cancellationToken)
    {
        var count = 0;

        var response = await httpClient.GetAsync($"/nifi-api/flow/process-groups/{processGroupId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get process group {ProcessGroupId}", processGroupId);
            return count;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(content);
        var processGroupFlow = doc.RootElement.GetProperty("processGroupFlow");
        var flow = processGroupFlow.GetProperty("flow");

        // Get process group name from breadcrumb
        string? processGroupName = null;
        if (processGroupFlow.TryGetProperty("breadcrumb", out var breadcrumb) &&
            breadcrumb.TryGetProperty("breadcrumb", out var innerBreadcrumb) &&
            innerBreadcrumb.TryGetProperty("name", out var nameElement))
        {
            processGroupName = nameElement.GetString();
        }

        // Build the full ancestor chain for processors in this group
        var currentAncestorChain = new List<(string id, string name)>(parentChain);
        if (!string.IsNullOrWhiteSpace(processGroupName))
        {
            currentAncestorChain.Add((processGroupId, processGroupName));
        }

        // Process processors in this group
        if (flow.TryGetProperty("processors", out var processors))
        {
            foreach (var processorElement in processors.EnumerateArray())
            {
                try
                {
                    await IngestProcessorAsync(processorElement, containerId, processGroupId, currentAncestorChain, httpClient, cancellationToken);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest processor from process group {ProcessGroupId}", processGroupId);
                }
            }
        }

        // Process connections for lineage in this group
        if (flow.TryGetProperty("connections", out var connections))
        {
            foreach (var connectionElement in connections.EnumerateArray())
            {
                try
                {
                    await IngestConnectionAsync(connectionElement, containerId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest connection from process group {ProcessGroupId}", processGroupId);
                }
            }
        }

        // Recursively process child process groups
        if (flow.TryGetProperty("processGroups", out var childProcessGroups))
        {
            foreach (var childPgElement in childProcessGroups.EnumerateArray())
            {
                try
                {
                    var pgComponent = childPgElement.GetProperty("component");
                    var childPgId = pgComponent.GetProperty("id").GetString();
                    
                    if (childPgId != null)
                    {
                        // Pass the full ancestor chain to child process group
                        count += await IngestProcessGroupAsync(httpClient, containerId, childPgId, currentAncestorChain, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest child process group");
                }
            }
        }

        return count;
    }

    private async Task IngestProcessorAsync(
        JsonElement processorElement,
        string containerId,
        string parentProcessGroupId,
        List<(string id, string name)> ancestorChain,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var component = processorElement.GetProperty("component");
        var processorId = component.GetProperty("id").GetString()!;
        var name = component.GetProperty("name").GetString()!;
        var type = component.GetProperty("type").GetString()!;
        var state = component.GetProperty("state").GetString();

        // Get processor config
        var config = component.GetProperty("config");
        var properties = new Dictionary<string, string>();
        
        // Get property values
        if (config.TryGetProperty("properties", out var propsElement))
        {
            foreach (var prop in propsElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    properties[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
                else if (prop.Value.ValueKind == JsonValueKind.Null)
                {
                    properties[prop.Name] = string.Empty;
                }
                else if (prop.Value.ValueKind == JsonValueKind.Number)
                {
                    properties[prop.Name] = prop.Value.ToString();
                }
                else if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                {
                    properties[prop.Name] = prop.Value.GetBoolean().ToString().ToLower();
                }
            }
        }

        // Get property descriptors (metadata about each property)
        if (config.TryGetProperty("descriptors", out var descriptorsElement))
        {
            foreach (var descriptor in descriptorsElement.EnumerateObject())
            {
                var propName = descriptor.Name;
                var descriptorObj = descriptor.Value;
                
                // Store descriptor metadata with property name prefix
                if (descriptorObj.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String)
                {
                    properties[$"{propName}__description"] = desc.GetString() ?? string.Empty;
                }
                if (descriptorObj.TryGetProperty("defaultValue", out var defaultVal) && defaultVal.ValueKind == JsonValueKind.String)
                {
                    properties[$"{propName}__default"] = defaultVal.GetString() ?? string.Empty;
                }
                if (descriptorObj.TryGetProperty("required", out var required))
                {
                    properties[$"{propName}__required"] = required.GetBoolean().ToString().ToLower();
                }
                if (descriptorObj.TryGetProperty("sensitive", out var sensitive))
                {
                    properties[$"{propName}__sensitive"] = sensitive.GetBoolean().ToString().ToLower();
                }
            }
        }

        var description = config.TryGetProperty("comments", out var commentsElement)
            ? commentsElement.GetString() ?? string.Empty
            : string.Empty;

        // Create domain entity
        var fqn = ProcessorFqn.Create(containerId, processorId);
        var processorName = ProcessorName.Create(name);
        var processorType = ProcessorType.Parse(type);
        var parentProcessGroupFqn = ProcessGroupId.Parse($"nifi://container/{containerId}/process-group/{parentProcessGroupId}");

        var processor = NiFiProcessor.Create(
            fqn,
            processorName,
            processorType,
            parentProcessGroupFqn);

        // Set additional properties
        if (!string.IsNullOrWhiteSpace(description))
        {
            processor.UpdateDescription(description);
        }

        // Store the full ancestor chain as JSON
        properties["parentProcessGroupId"] = parentProcessGroupId;
        
        if (ancestorChain != null && ancestorChain.Count > 0)
        {
            // Convert tuples to anonymous objects for proper JSON serialization
            var ancestorObjects = ancestorChain.Select(a => new { id = a.id, name = a.name }).ToList();
            var ancestorJson = System.Text.Json.JsonSerializer.Serialize(ancestorObjects);
            properties["ancestorChain"] = ancestorJson;
            
            // Also store immediate parent name for backward compatibility
            var immediateParent = ancestorChain[ancestorChain.Count - 1];
            properties["parentProcessGroupName"] = immediateParent.name;
        }

        processor.UpdateProperties(ProcessorProperties.Create(properties));

        // Update status based on NiFi state
        var status = state?.ToLowerInvariant() switch
        {
            "running" => ProcessorStatus.Active,
            "stopped" => ProcessorStatus.Inactive,
            "disabled" => ProcessorStatus.Inactive,
            _ => ProcessorStatus.Active
        };

        if (status == ProcessorStatus.Inactive)
        {
            processor.Deactivate();
        }

        // Save to OpenSearch ONLY (processors are not stored in ArangoDB)
        var searchResult = await _searchRepository.IndexEntityAsync(processor, cancellationToken);

        if (!searchResult.IsSuccess)
        {
            _logger.LogWarning("Failed to index processor {ProcessorId} in OpenSearch: {Error} - continuing with column extraction", processorId, searchResult.Error);
        }
        else
        {
            _logger.LogDebug("Ingested processor: {ProcessorName} ({ProcessorId})", name, processorId);
        }

        // Extract and ingest columns (columns ARE stored in ArangoDB) - always run this
        await IngestProcessorColumnsAsync(processor, type, properties, containerId, processorId, httpClient, cancellationToken);
    }

    private async Task IngestConnectionAsync(
        JsonElement connectionElement,
        string containerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var component = connectionElement.GetProperty("component");
            var source = component.GetProperty("source");
            var destination = component.GetProperty("destination");

            var sourceId = source.GetProperty("id").GetString();
            var destinationId = destination.GetProperty("id").GetString();

            if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(destinationId))
            {
                return;
            }

            // Create FQNs for source and destination
            var sourceFqn = ProcessorFqn.Create(containerId, sourceId).Value;
            var destinationFqn = ProcessorFqn.Create(containerId, destinationId).Value;

            _logger.LogDebug("Processing connection: {Source} -> {Destination}", sourceId, destinationId);
                
            // Add column-level lineage (processor-level lineage is derived from OpenSearch)
            await IngestColumnLineageAsync(sourceFqn, destinationFqn, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting connection");
        }
    }

    private string? FindNiFiPort(ContainerInfo containerInfo)
    {
        try
        {
            // ContainerInfo.Ports is a Dictionary<string, string> where key is "8080/tcp" and value is the HostPort
            if (containerInfo.Ports.TryGetValue("8080/tcp", out var hostPort))
            {
                return hostPort;
            }

            _logger.LogWarning("No port mapping found for 8080/tcp in container {ContainerId}", containerInfo.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding NiFi port");
        }

        return null;
    }

    private async Task IngestProcessorColumnsAsync(
        NiFiProcessor processor,
        string processorType,
        Dictionary<string, string> properties,
        string containerId,
        string processorId,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Extracting columns for processor {ProcessorId} of type {ProcessorType}", processorId, processorType);
            var columns = new List<SchemaColumn>();
            var processorFqn = ProcessorFqn.Create(containerId, processorId);

            // Strategy 1: Extract from schema properties (Avro/JSON)
            var schemaColumns = _schemaExtractor.ExtractFromProcessorProperties(properties);
            _logger.LogDebug("Schema extractor found {Count} columns", schemaColumns.Count);
            columns.AddRange(schemaColumns);

            // Strategy 2: For SQL processors, use GSP to parse SQL and extract columns
            if (processorType.Contains("ExecuteSQL", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Processor is ExecuteSQL type, looking for SQL query");
                var sqlQuery = properties.GetValueOrDefault("SQL select query");
                _logger.LogDebug("SQL query found: {HasQuery}, Length: {Length}", 
                    !string.IsNullOrWhiteSpace(sqlQuery), 
                    sqlQuery?.Length ?? 0);
                
                if (!string.IsNullOrWhiteSpace(sqlQuery))
                {
                    var vendor = DetermineVendor(properties);
                    _logger.LogInformation("Parsing SQL with vendor: {Vendor}", vendor);
                    var gspResult = await _gspParser.ParseSqlAsync(sqlQuery, vendor, cancellationToken);

                    if (gspResult != null)
                    {
                        _logger.LogInformation("GSP returned {TableCount} tables and {ColumnCount} columns", 
                            gspResult.SourceTables.Count, 
                            gspResult.ResultColumns.Count);
                        
                        // Ingest database tables and columns
                        foreach (var table in gspResult.SourceTables)
                        {
                            await IngestDatabaseTableAsync(table, vendor, cancellationToken);
                        }

                        // Add result columns to processor columns
                        columns.AddRange(gspResult.ResultColumns.Select(c => new SchemaColumn
                        {
                            Name = c.Name
                        }));
                        
                        // Store GSP lineage for creating edges after processor columns are indexed
                        var dbcpService = properties.GetValueOrDefault("Database Connection Pooling Service");
                        _gspLineageCache[processorFqn.Value] = (gspResult, vendor, dbcpService);
                    }
                    else
                    {
                        _logger.LogWarning("GSP parser returned null for SQL query");
                    }
                }
                else
                {
                    _logger.LogWarning("ExecuteSQL processor has no SQL query configured");
                }
            }
            else if (processorType.Contains("PutDatabaseRecord", StringComparison.OrdinalIgnoreCase))
            {
                // NiFi uses prefixed property keys for PutDatabaseRecord
                var tableName = properties.GetValueOrDefault("put-db-record-table-name")
                    ?? properties.GetValueOrDefault("Table Name");
                var catalogName = properties.GetValueOrDefault("put-db-record-catalog-name")
                    ?? properties.GetValueOrDefault("Catalog Name");
                var schemaName = properties.GetValueOrDefault("put-db-record-schema-name")
                    ?? properties.GetValueOrDefault("Schema Name");
                var dbcpServiceId = properties.GetValueOrDefault("put-db-record-dcbp-service")
                    ?? properties.GetValueOrDefault("Database Connection Pooling Service");

                _logger.LogInformation(
                    "PutDatabaseRecord table info: catalog={Catalog}, schema={Schema}, table={Table}, dbcp={Dbcp}",
                    catalogName, schemaName, tableName, dbcpServiceId);

                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    // Fetch JDBC URL from controller service to determine vendor/platform
                    var vendor = await DetermineVendorFromControllerServiceAsync(dbcpServiceId, properties, httpClient, cancellationToken);
                    await IngestDatabaseTableFromNameAsync(tableName, vendor, cancellationToken);

                    // Store table info for later use (after column propagation)
                    _processorTableNameCache[processorFqn.Value] = (tableName, catalogName, schemaName, vendor, dbcpServiceId);
                }
            }

            // Index columns in OpenSearch and ArangoDB
            if (columns.Count == 0)
            {
                _logger.LogWarning("No columns found for processor {ProcessorId} of type {ProcessorType}", processorId, processorType);
            }
            else
            {
                _logger.LogInformation("Indexing {Count} columns for processor {ProcessorId}", columns.Count, processorId);
            }
            
            foreach (var column in columns)
            {
                var columnFqn = ColumnFqn.CreateFromProcessor(processorFqn, column.Name);
                var nifiColumn = NiFiColumn.Create(
                    columnFqn,
                    column.Name,
                    processorFqn,
                    column.DataType,
                    column.Description,
                    column.IsNullable,
                    column.OrdinalPosition);

                _logger.LogInformation("Indexing column {ColumnName} with URN {Urn}", column.Name, columnFqn.Value);

                // Index in OpenSearch (full metadata)
                await _searchRepository.IndexColumnAsync(
                    nifiColumn,
                    columnFqn.Value,
                    "COLUMN",
                    "NiFi",
                    processorFqn.Value,
                    cancellationToken);

                // Add to ArangoDB (URN only)
                _logger.LogInformation("Adding column vertex to ArangoDB: {Urn}", columnFqn.Value);
                await _graphRepository.AddColumnVertexAsync(
                    columnFqn.Value,
                    cancellationToken);
            }

            // Cache columns for lineage mapping
            _processorColumnsCache[processorFqn.Value] = columns;

            // If this processor has GSP lineage, create table-to-processor edges
            if (_gspLineageCache.TryGetValue(processorFqn.Value, out var gspInfo))
            {
                await CreateTableToProcessorEdgesAsync(processorFqn, gspInfo.Result, gspInfo.Vendor, gspInfo.DbcpService, cancellationToken);
            }

            _logger.LogInformation("Completed column ingestion for processor {ProcessorId}: {Count} columns", processorId, columns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest columns for processor {ProcessorId}", processorId);
        }
    }

    private async Task IngestColumnLineageAsync(
        string sourceFqn,
        string destinationFqn,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get columns from cache
            if (!_processorColumnsCache.TryGetValue(sourceFqn, out var sourceColumns))
            {
                _logger.LogDebug("No columns cached for source processor {SourceFqn}", sourceFqn);
                return;
            }

            // If destination has no columns, propagate from source
            if (!_processorColumnsCache.TryGetValue(destinationFqn, out var targetColumns) || targetColumns.Count == 0)
            {
                _logger.LogInformation("Destination processor {DestinationFqn} has no columns - propagating {Count} columns from source", 
                    destinationFqn, sourceColumns.Count);
                
                // Propagate columns to destination processor
                var destinationProcessorFqn = ProcessorFqn.Parse(destinationFqn);
                foreach (var column in sourceColumns)
                {
                    var columnFqn = ColumnFqn.CreateFromProcessor(destinationProcessorFqn, column.Name);
                    var nifiColumn = NiFiColumn.Create(
                        columnFqn,
                        column.Name,
                        destinationProcessorFqn,
                        column.DataType,
                        column.Description,
                        column.IsNullable,
                        column.OrdinalPosition);

                    // Index in OpenSearch
                    await _searchRepository.IndexColumnAsync(
                        nifiColumn,
                        columnFqn.Value,
                        "COLUMN",
                        "NiFi",
                        destinationProcessorFqn.Value,
                        cancellationToken);

                    // Add to ArangoDB
                    _logger.LogInformation("Adding propagated column vertex to ArangoDB: {Urn}", columnFqn.Value);
                    await _graphRepository.AddColumnVertexAsync(
                        columnFqn.Value,
                        cancellationToken);
                }
                
                // Cache the propagated columns
                targetColumns = sourceColumns;
                _processorColumnsCache[destinationFqn] = targetColumns;
                
                _logger.LogInformation("Propagated {Count} columns to processor {DestinationFqn}", 
                    sourceColumns.Count, destinationFqn);
                
                // If destination is a PutDatabaseRecord with a table name, create target table columns and edges
                if (_processorTableNameCache.TryGetValue(destinationFqn, out var tableInfo))
                {
                    _logger.LogInformation("Creating target table columns for {TableName}", tableInfo.TableName);
                    await CreateTargetTableColumnsAndEdgesAsync(
                        destinationFqn, 
                        tableInfo.TableName,
                        tableInfo.CatalogName,
                        tableInfo.SchemaName,
                        tableInfo.Vendor, 
                        sourceColumns, 
                        cancellationToken);
                }
            }

            // Map columns between processors
            var sourceProcessorFqn = ProcessorFqn.Parse(sourceFqn);
            var targetProcessorFqn = ProcessorFqn.Parse(destinationFqn);

            var columnMappings = _columnMapper.MapColumns(
                "Unknown", // We don't have processor type here, but mapper uses name matching
                "Unknown",
                sourceColumns,
                targetColumns);

            // Create column lineage edges
            foreach (var mapping in columnMappings)
            {
                var sourceColumnFqn = ColumnFqn.CreateFromProcessor(sourceProcessorFqn, mapping.SourceColumnName);
                var targetColumnFqn = ColumnFqn.CreateFromProcessor(targetProcessorFqn, mapping.TargetColumnName);

                await _graphRepository.AddColumnLineageEdgeAsync(
                    sourceColumnFqn.Value,
                    targetColumnFqn.Value,
                    RelationshipType.Lineage,
                    cancellationToken);

                _logger.LogDebug(
                    "Added column lineage: {SourceColumn} -> {TargetColumn}",
                    mapping.SourceColumnName,
                    mapping.TargetColumnName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest column lineage from {Source} to {Destination}", sourceFqn, destinationFqn);
        }
    }

    private async Task IngestDatabaseTableAsync(
        GSPTable gspTable,
        string vendor,
        CancellationToken cancellationToken)
    {
        try
        {
            var server = "localhost"; // TODO: Parse from JDBC URL in properties
            var database = gspTable.Database ?? "unknown";
            var schema = gspTable.Schema ?? "dbo";
            var tableName = gspTable.Name;
            var platform = MapVendorToPlatform(vendor);

            // Create database entity
            var dbFqn = DatabaseFqn.CreateDatabase(server, database);
            await _searchRepository.IndexDatabaseEntityAsync(
                dbFqn.Value,
                database,
                "DATABASE",
                platform,
                null,
                new Dictionary<string, string> { ["server"] = server },
                cancellationToken);

            // Create schema entity
            var schemaFqn = DatabaseFqn.CreateSchema(server, database, schema);
            await _searchRepository.IndexDatabaseEntityAsync(
                schemaFqn.Value,
                schema,
                "SCHEMA",
                platform,
                dbFqn.Value,
                new Dictionary<string, string> { ["database"] = database },
                cancellationToken);

            // Create table entity
            var tableFqn = DatabaseFqn.CreateTable(server, database, schema, tableName);
            var table = DatabaseTable.Create(
                tableFqn,
                tableName,
                server,
                database,
                schema,
                platform);

            // Index table in OpenSearch ONLY (not in ArangoDB)
            await _searchRepository.IndexTableAsync(table, cancellationToken);

            // Create column entities
            foreach (var columnName in gspTable.Columns)
            {
                var columnFqn = DatabaseFqn.CreateColumn(server, database, schema, tableName, columnName);
                var column = DatabaseColumn.Create(
                    columnFqn,
                    columnName,
                    tableFqn,
                    "VARCHAR", // Default type, GSP doesn't provide this
                    true,
                    0);

                // Index in OpenSearch (full metadata)
                await _searchRepository.IndexColumnAsync(
                    column,
                    columnFqn.Value,
                    "COLUMN",
                    platform,
                    tableFqn.Value,
                    cancellationToken);

                // Add to ArangoDB (URN only)
                await _graphRepository.AddColumnVertexAsync(
                    columnFqn.Value,
                    cancellationToken);
            }

            _logger.LogDebug("Ingested database table {TableName} with {ColumnCount} columns", tableName, gspTable.Columns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest database table {TableName}", gspTable.Name);
        }
    }

    private async Task IngestDatabaseTableFromNameAsync(
        string tableName,
        string vendor,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse table name (format: schema.table or just table)
            var parts = tableName.Split('.');
            var schema = parts.Length > 1 ? parts[0] : "dbo";
            var table = parts.Length > 1 ? parts[1] : parts[0];

            var server = "localhost";
            var database = "unknown";
            var platform = MapVendorToPlatform(vendor);

            // Create minimal table entity (without columns since we don't have the schema)
            var tableFqn = DatabaseFqn.CreateTable(server, database, schema, table);
            var tableEntity = DatabaseTable.Create(
                tableFqn,
                table,
                server,
                database,
                schema,
                platform);

            // Index table in OpenSearch ONLY (not in ArangoDB)
            await _searchRepository.IndexTableAsync(tableEntity, cancellationToken);

            _logger.LogDebug("Ingested database table {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest database table from name {TableName}", tableName);
        }
    }

    private async Task CreateTargetTableColumnsAndEdgesAsync(
        string processorFqn,
        string tableName,
        string? catalogName,
        string? schemaName,
        string vendor,
        List<SchemaColumn> columns,
        CancellationToken cancellationToken)
    {
        try
        {
            // Use provided catalog/schema or parse from table name
            var database = catalogName ?? "unknown";
            var schema = schemaName ?? "dbo";
            var table = tableName;

            var server = "localhost";
            var platform = MapVendorToPlatform(vendor);

            var tableFqn = DatabaseFqn.CreateTable(server, database, schema, table);
            var parsedProcessorFqn = ProcessorFqn.Parse(processorFqn);

            // Index the target table entity itself
            var tableEntity = DatabaseTable.Create(tableFqn, table, server, database, schema, platform);
            await _searchRepository.IndexTableAsync(tableEntity, cancellationToken);
            _logger.LogInformation("Indexed target table entity: {TableFqn}", tableFqn.Value);

            // Create column entities and edges
            foreach (var column in columns)
            {
                var columnFqn = DatabaseFqn.CreateColumn(server, database, schema, table, column.Name);
                var dbColumn = DatabaseColumn.Create(
                    columnFqn,
                    column.Name,
                    tableFqn,
                    column.DataType ?? "VARCHAR",
                    column.IsNullable ?? true,
                    column.OrdinalPosition ?? 0);

                // Index in OpenSearch (full metadata)
                await _searchRepository.IndexColumnAsync(
                    dbColumn,
                    columnFqn.Value,
                    "COLUMN",
                    platform,
                    tableFqn.Value,
                    cancellationToken);

                // Add to ArangoDB (URN only)
                await _graphRepository.AddColumnVertexAsync(
                    columnFqn.Value,
                    cancellationToken);

                // Create edge from processor column to table column
                var processorColumnFqn = ColumnFqn.CreateFromProcessor(parsedProcessorFqn, column.Name);
                _logger.LogInformation("Creating processor-to-table edge: {Source} -> {Target}", 
                    processorColumnFqn.Value, columnFqn.Value);
                    
                await _graphRepository.AddColumnLineageEdgeAsync(
                    processorColumnFqn.Value,
                    columnFqn.Value,
                    RelationshipType.Lineage,
                    cancellationToken);
            }

            _logger.LogInformation("Created {Count} columns and edges for target table {TableName}", columns.Count, tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create target table columns for {TableName}", tableName);
        }
    }

    private async Task CreateTableToProcessorEdgesAsync(
        ProcessorFqn processorFqn,
        GSPLineageResult gspResult,
        string vendor,
        string? dbcpService,
        CancellationToken cancellationToken)
    {
        try
        {
            var platform = MapVendorToPlatform(vendor);
            
            // TODO: Extract database and schema from DBCP Service
            // For now, use GSP result or defaults
            
            // Create edges from source table columns to processor columns
            foreach (var lineage in gspResult.ColumnLineages)
            {
                // Parse source table info
                var sourceTable = gspResult.SourceTables.FirstOrDefault(t => t.Name.Contains(lineage.SourceTable));
                if (sourceTable == null) continue;

                var parts = sourceTable.Name.Split('.');
                var schema = sourceTable.Schema ?? (parts.Length > 1 ? parts[^2] : "dbo");
                var table = parts[^1];
                var server = "localhost";
                var database = sourceTable.Database ?? "unknown";

                // Create source column URN (from table)
                var sourceColumnFqn = DatabaseFqn.CreateColumn(server, database, schema, table, lineage.SourceColumn);
                
                // Create target column URN (processor column)
                var targetColumnFqn = ColumnFqn.CreateFromProcessor(processorFqn, lineage.TargetColumn);

                // Create edge from table column to processor column
                _logger.LogInformation("Creating table-to-processor edge: {Source} -> {Target}", 
                    sourceColumnFqn.Value, targetColumnFqn.Value);
                    
                await _graphRepository.AddColumnLineageEdgeAsync(
                    sourceColumnFqn.Value,
                    targetColumnFqn.Value,
                    RelationshipType.Lineage,
                    cancellationToken);
            }

            _logger.LogInformation("Created {Count} table-to-processor edges for processor {ProcessorId}", 
                gspResult.ColumnLineages.Count, processorFqn.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create table-to-processor edges");
        }
    }

    private async Task<string> DetermineVendorFromControllerServiceAsync(
        string? controllerServiceId,
        Dictionary<string, string> processorProperties,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        // First try direct properties
        var vendorFromProps = DetermineVendor(processorProperties);
        if (vendorFromProps != "dbvmssql")
            return vendorFromProps;

        // If no direct URL, try fetching the controller service
        if (!string.IsNullOrWhiteSpace(controllerServiceId))
        {
            try
            {
                var response = await httpClient.GetAsync(
                    $"/nifi-api/controller-services/{controllerServiceId}",
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var doc = JsonDocument.Parse(content);
                    var component = doc.RootElement.GetProperty("component");

                    // Extract JDBC URL from controller service properties
                    if (component.TryGetProperty("properties", out var csProps))
                    {
                        string? jdbcUrl = null;
                        foreach (var prop in csProps.EnumerateObject())
                        {
                            if ((prop.Name.Contains("url", StringComparison.OrdinalIgnoreCase) ||
                                 prop.Name.Contains("connection", StringComparison.OrdinalIgnoreCase)) &&
                                prop.Value.ValueKind == JsonValueKind.String)
                            {
                                var val = prop.Value.GetString() ?? string.Empty;
                                if (val.StartsWith("jdbc:", StringComparison.OrdinalIgnoreCase))
                                {
                                    jdbcUrl = val;
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(jdbcUrl))
                        {
                            _logger.LogInformation("Controller service {Id} JDBC URL: {Url}", controllerServiceId, jdbcUrl);
                            return DetermineVendorFromJdbcUrl(jdbcUrl);
                        }

                        // Also check type name for Snowflake
                        if (component.TryGetProperty("type", out var typeEl))
                        {
                            var typeName = typeEl.GetString() ?? string.Empty;
                            if (typeName.Contains("Snowflake", StringComparison.OrdinalIgnoreCase))
                                return "dbvsnowflake";
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to fetch controller service {Id}: {Status}", controllerServiceId, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching controller service {Id}", controllerServiceId);
            }
        }

        return vendorFromProps;
    }

    private string DetermineVendorFromJdbcUrl(string jdbcUrl)
    {
        if (jdbcUrl.Contains("sqlserver", StringComparison.OrdinalIgnoreCase))
            return "dbvmssql";
        if (jdbcUrl.Contains("postgresql", StringComparison.OrdinalIgnoreCase))
            return "dbvpostgresql";
        if (jdbcUrl.Contains("mysql", StringComparison.OrdinalIgnoreCase))
            return "dbvmysql";
        if (jdbcUrl.Contains("oracle", StringComparison.OrdinalIgnoreCase))
            return "dbvoracle";
        if (jdbcUrl.Contains("snowflake", StringComparison.OrdinalIgnoreCase))
            return "dbvsnowflake";
        return "dbvmssql";
    }

    private string DetermineVendor(Dictionary<string, string> properties)
    {
        // Try to determine vendor from JDBC URL or connection pool properties
        var jdbcUrl = properties.GetValueOrDefault("Database Connection URL") ?? string.Empty;
        return DetermineVendorFromJdbcUrl(jdbcUrl);
    }

    private string MapVendorToPlatform(string vendor)
    {
        return vendor.ToLowerInvariant() switch
        {
            "dbvmssql" => "MSSQL",
            "dbvpostgresql" => "PostgreSQL",
            "dbvmysql" => "MySQL",
            "dbvoracle" => "Oracle",
            "dbvsnowflake" => "Snowflake",
            _ => "MSSQL"
        };
    }
}
