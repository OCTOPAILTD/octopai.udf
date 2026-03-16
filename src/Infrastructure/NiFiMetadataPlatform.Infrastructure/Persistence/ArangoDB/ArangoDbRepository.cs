using ArangoDBNetStandard;
using ArangoDBNetStandard.CollectionApi.Models;
using ArangoDBNetStandard.DatabaseApi.Models;
using ArangoDBNetStandard.GraphApi.Models;
using ArangoDBNetStandard.Transport.Http;
using Microsoft.Extensions.Logging;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Common;
using NiFiMetadataPlatform.Domain.Enums;
using NiFiMetadataPlatform.Infrastructure.Configuration;

namespace NiFiMetadataPlatform.Infrastructure.Persistence.ArangoDB;

/// <summary>
/// ArangoDB implementation of the graph repository.
/// Stores ONLY column vertices (URN only) and column lineage edges.
/// All other metadata is stored in OpenSearch.
/// </summary>
public sealed class ArangoDbRepository : IGraphRepository
{
    private readonly ArangoDbSettings _settings;
    private readonly ILogger<ArangoDbRepository> _logger;
    private readonly Lazy<Task<ArangoDBClient>> _clientLazy;

    private const string ColumnVertexCollection = "columns";
    private const string ColumnEdgeCollection = "column_lineage";
    private const string GraphName = "column_lineage_graph";

    /// <summary>
    /// Initializes a new instance of the <see cref="ArangoDbRepository"/> class.
    /// </summary>
    /// <param name="settings">The ArangoDB settings.</param>
    /// <param name="logger">The logger.</param>
    public ArangoDbRepository(
        ArangoDbSettings settings,
        ILogger<ArangoDbRepository> logger)
    {
        _settings = settings;
        _logger = logger;
        _clientLazy = new Lazy<Task<ArangoDBClient>>(InitializeClientAsync);
    }

    private async Task<ArangoDBClient> InitializeClientAsync()
    {
        try
        {
            // First, connect to _system database to create our database if needed
            var systemTransport = HttpApiTransport.UsingBasicAuth(
                new Uri(_settings.Endpoint),
                "_system",
                _settings.Username,
                _settings.Password);

            var systemClient = new ArangoDBClient(systemTransport);

            // Check if our database exists, create if not
            try
            {
                var databases = await systemClient.Database.GetDatabasesAsync();
                if (!databases.Result.Contains(_settings.Database))
                {
                    _logger.LogInformation("Creating ArangoDB database: {Database}", _settings.Database);
                    await systemClient.Database.PostDatabaseAsync(new PostDatabaseBody
                    {
                        Name = _settings.Database
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check/create database, assuming it exists");
            }

            // Now connect to our database
            var transport = HttpApiTransport.UsingBasicAuth(
                new Uri(_settings.Endpoint),
                _settings.Database,
                _settings.Username,
                _settings.Password);

            var client = new ArangoDBClient(transport);
            
            _logger.LogInformation("Connected to ArangoDB database: {Database}", _settings.Database);

            // Ensure collections exist
            await EnsureCollectionsExistAsync(client);

            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ArangoDB client");
            throw;
        }
    }

    private async Task EnsureCollectionsExistAsync(ArangoDBClient client)
    {
        try
        {
            // Create columns vertex collection
            await CreateCollectionIfNotExistsAsync(client, ColumnVertexCollection, CollectionType.Document);

            // Create column_lineage edge collection
            await CreateCollectionIfNotExistsAsync(client, ColumnEdgeCollection, CollectionType.Edge);

            // Create graph
            try
            {
                await client.Graph.PostGraphAsync(new PostGraphBody
                {
                    Name = GraphName,
                    EdgeDefinitions = new List<EdgeDefinition>
                    {
                        new EdgeDefinition
                        {
                            Collection = ColumnEdgeCollection,
                            From = new[] { ColumnVertexCollection },
                            To = new[] { ColumnVertexCollection }
                        }
                    }
                });
                _logger.LogInformation("Created graph: {Graph}", GraphName);
            }
            catch
            {
                // Graph already exists
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error ensuring collections exist");
        }
    }

    private async Task CreateCollectionIfNotExistsAsync(ArangoDBClient client, string collectionName, CollectionType type)
    {
        try
        {
            await client.Collection.PostCollectionAsync(new PostCollectionBody
            {
                Name = collectionName,
                Type = type
            });
            _logger.LogInformation("Created {Type} collection: {Collection}", type, collectionName);
        }
        catch
        {
            // Collection already exists
        }
    }

    /// <inheritdoc/>
    public async Task<Result> AddColumnVertexAsync(string columnUrn, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding column vertex: {Urn}", columnUrn);
            
            var client = await _clientLazy.Value;
            
            // Create minimal vertex document (URN only)
            var vertex = new
            {
                _key = SanitizeKey(columnUrn),
                urn = columnUrn,
                createdAt = DateTime.UtcNow
            };

            // Upsert: Insert or update the vertex
            await client.Document.PostDocumentAsync(
                ColumnVertexCollection,
                vertex,
                new ArangoDBNetStandard.DocumentApi.Models.PostDocumentsQuery
                {
                    Overwrite = true,
                    ReturnNew = false
                });

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add column vertex: {Urn}", columnUrn);
            return Result.Failure($"Failed to add column vertex: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<bool> EdgeExistsAsync(
        string fromColumnUrn,
        string toColumnUrn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _clientLazy.Value;
            
            var aql = @"
                FOR e IN @@collection
                    FILTER e.fromUrn == @from AND e.toUrn == @to
                    LIMIT 1
                    RETURN e
            ";

            var bindVars = new Dictionary<string, object>
            {
                ["@collection"] = ColumnEdgeCollection,
                ["from"] = fromColumnUrn,
                ["to"] = toColumnUrn
            };

            var cursor = await client.Cursor.PostCursorAsync<object>(aql, bindVars);

            return cursor.Result?.Any() ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if edge exists");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<Result> AddColumnLineageEdgeAsync(
        string fromColumnUrn,
        string toColumnUrn,
        RelationshipType relationshipType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if edge already exists (idempotent)
            if (await EdgeExistsAsync(fromColumnUrn, toColumnUrn, cancellationToken))
            {
                _logger.LogDebug("Column lineage edge already exists: {From} -> {To}", fromColumnUrn, toColumnUrn);
                return Result.Success();
            }

            _logger.LogInformation("Adding column lineage edge: {From} -> {To}", fromColumnUrn, toColumnUrn);
            
            var client = await _clientLazy.Value;
            
            var fromKey = SanitizeKey(fromColumnUrn);
            var toKey = SanitizeKey(toColumnUrn);
            
            // Create edge document
            var edge = new
            {
                _from = $"{ColumnVertexCollection}/{fromKey}",
                _to = $"{ColumnVertexCollection}/{toKey}",
                relationshipType = relationshipType.ToString(),
                fromUrn = fromColumnUrn,
                toUrn = toColumnUrn,
                createdAt = DateTime.UtcNow
            };

            // Insert edge
            await client.Document.PostDocumentAsync(
                ColumnEdgeCollection,
                edge);

            _logger.LogDebug("Added column lineage edge: {From} -> {To}", fromColumnUrn, toColumnUrn);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add column lineage edge: {From} -> {To}", fromColumnUrn, toColumnUrn);
            return Result.Failure($"Failed to add column lineage edge: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<List<string>>> TraverseColumnLineageAsync(
        string columnUrn,
        int depth,
        LineageDirection direction,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Traversing column lineage for {Urn}, direction: {Direction}, depth: {Depth}", 
                columnUrn, direction, depth);
            
            var client = await _clientLazy.Value;
            var startKey = SanitizeKey(columnUrn);
            
            // Build AQL query for graph traversal
            var directionKeyword = direction == LineageDirection.Upstream ? "INBOUND" : "OUTBOUND";
            
            var aql = $@"
                FOR v, e, p IN 1..{depth} {directionKeyword} 
                    '{ColumnVertexCollection}/{startKey}' 
                    GRAPH '{GraphName}'
                    RETURN DISTINCT v.urn
            ";

            var cursor = await client.Cursor.PostCursorAsync<string>(aql);

            var results = new List<string>();
            
            // Collect all results from cursor
            if (cursor.Result != null)
            {
                results.AddRange(cursor.Result);
            }
            
            // Fetch remaining batches if any
            while (cursor.HasMore)
            {
                var nextBatch = await client.Cursor.PutCursorAsync<string>(cursor.Id);
                if (nextBatch.Result != null)
                {
                    results.AddRange(nextBatch.Result);
                }
                cursor = new ArangoDBNetStandard.CursorApi.Models.CursorResponse<string>
                {
                    HasMore = nextBatch.HasMore,
                    Id = nextBatch.Id,
                    Result = nextBatch.Result
                };
            }

            _logger.LogInformation("Found {Count} {Direction} columns for {Urn}", results.Count, direction, columnUrn);
            
            return Result<List<string>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to traverse column lineage for {Urn}", columnUrn);
            return Result<List<string>>.Failure($"Failed to traverse column lineage: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<List<(string From, string To)>>> GetDirectColumnEdgesAsync(
        IEnumerable<string> columnUrns,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var urnList = columnUrns.ToList();
            if (urnList.Count == 0)
            {
                return Result<List<(string From, string To)>>.Success(new List<(string, string)>());
            }

            var client = await _clientLazy.Value;

            // Query all edges where fromUrn OR toUrn is in our list
            var aql = @"
                FOR e IN @@collection
                    FILTER e.fromUrn IN @urns OR e.toUrn IN @urns
                    RETURN { from: e.fromUrn, to: e.toUrn }
            ";

            var bindVars = new Dictionary<string, object>
            {
                ["@collection"] = ColumnEdgeCollection,
                ["urns"] = urnList
            };

            var cursor = await client.Cursor.PostCursorAsync<EdgeResult>(aql, bindVars);

            var results = new List<(string From, string To)>();

            if (cursor.Result != null)
            {
                results.AddRange(cursor.Result.Select(r => (r.From, r.To)));
            }

            while (cursor.HasMore)
            {
                var nextBatch = await client.Cursor.PutCursorAsync<EdgeResult>(cursor.Id);
                if (nextBatch.Result != null)
                {
                    results.AddRange(nextBatch.Result.Select(r => (r.From, r.To)));
                }
                cursor = new ArangoDBNetStandard.CursorApi.Models.CursorResponse<EdgeResult>
                {
                    HasMore = nextBatch.HasMore,
                    Id = nextBatch.Id,
                    Result = nextBatch.Result
                };
            }

            _logger.LogInformation("Found {Count} direct edges for {UrnCount} columns", results.Count, urnList.Count);
            return Result<List<(string From, string To)>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get direct column edges");
            return Result<List<(string From, string To)>>.Failure($"Failed to get direct column edges: {ex.Message}");
        }
    }

    private sealed class EdgeResult
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
    }

    private static string SanitizeKey(string urn)
    {
        // ArangoDB keys can only contain: a-z, A-Z, 0-9, _, -, :, @, (, ), +, ,, =, ;, $, !, *, ', %
        // Replace / with _ and other special chars
        return urn
            .Replace("://", "_")
            .Replace("/", "_")
            .Replace("{", "_")
            .Replace("}", "_");
    }
}
