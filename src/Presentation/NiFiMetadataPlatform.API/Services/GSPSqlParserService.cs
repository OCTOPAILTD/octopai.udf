using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace NiFiMetadataPlatform.API.Services;

/// <summary>
/// Service for parsing SQL queries using the GSP (Gudu SQLFlow) API.
/// </summary>
public interface IGSPSqlParserService
{
    /// <summary>
    /// Parses a SQL query and extracts column-level lineage.
    /// </summary>
    /// <param name="sql">The SQL query to parse.</param>
    /// <param name="vendor">The database vendor (e.g., "dbvmssql", "dbvpostgresql").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed lineage result.</returns>
    Task<GSPLineageResult?> ParseSqlAsync(string sql, string vendor, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of GSP SQL parsing.
/// </summary>
public sealed class GSPLineageResult
{
    public List<GSPTable> SourceTables { get; set; } = new();
    public List<GSPColumn> ResultColumns { get; set; } = new();
    public List<GSPColumnLineage> ColumnLineages { get; set; } = new();
}

/// <summary>
/// Represents a table in GSP response.
/// </summary>
public sealed class GSPTable
{
    public string Name { get; set; } = string.Empty;
    public string? Database { get; set; }
    public string? Schema { get; set; }
    public List<string> Columns { get; set; } = new();
}

/// <summary>
/// Represents a column in GSP response.
/// </summary>
public sealed class GSPColumn
{
    public string Name { get; set; } = string.Empty;
    public string? ParentName { get; set; }
}

/// <summary>
/// Represents a column-to-column lineage relationship.
/// </summary>
public sealed class GSPColumnLineage
{
    public string SourceTable { get; set; } = string.Empty;
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetColumn { get; set; } = string.Empty;
}

/// <summary>
/// Implementation of GSP SQL parser service.
/// </summary>
public sealed class GSPSqlParserService : IGSPSqlParserService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GSPSqlParserService> _logger;
    private readonly string _gspUrl;
    private readonly int _timeout;

    public GSPSqlParserService(
        IHttpClientFactory httpClientFactory,
        ILogger<GSPSqlParserService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _gspUrl = configuration["GSP:Url"] ?? "http://gsp.onprem.qa.octopai-corp.local/parser/gsp/dataflow";
        _timeout = int.Parse(configuration["GSP:Timeout"] ?? "30");
    }

    public async Task<GSPLineageResult?> ParseSqlAsync(string sql, string vendor, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calling GSP API for vendor: {Vendor}", vendor);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(_timeout);

            var payload = new
            {
                query = sql,
                vendor = vendor  // Keep vendor as-is (should be lowercase like "dbvmssql")
            };

            var response = await httpClient.PostAsJsonAsync(_gspUrl, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GSP API error: {StatusCode} - {Content}. Trying fallback parser.", 
                    response.StatusCode, errorContent[..Math.Min(500, errorContent.Length)]);
                return ParseSqlFallback(sql);
            }

            var xmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("GSP API response received (length: {Length})", xmlContent.Length);
            
            // Log the actual response for debugging
            if (xmlContent.Length < 100)
            {
                _logger.LogWarning("GSP API returned short response: {Response}. Trying fallback parser.", xmlContent);
                return ParseSqlFallback(sql);
            }

            var gspResult = ParseGSPXmlResponse(xmlContent);
            
            // If GSP returned empty results, try fallback
            if (gspResult.ResultColumns.Count == 0 && gspResult.SourceTables.Count == 0)
            {
                _logger.LogWarning("GSP returned no results. Trying fallback parser.");
                return ParseSqlFallback(sql);
            }
            
            return gspResult;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "GSP API timeout after {Timeout}s. Trying fallback parser.", _timeout);
            return ParseSqlFallback(sql);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GSP API call failed. Trying fallback parser.");
            return ParseSqlFallback(sql);
        }
    }

    /// <summary>
    /// Fallback: Simple SQL parser for basic SELECT statements when GSP fails.
    /// </summary>
    private GSPLineageResult? ParseSqlFallback(string sql)
    {
        try
        {
            _logger.LogInformation("Using fallback SQL parser for simple SELECT statements");
            
            // Remove comments and normalize whitespace
            var cleanSql = System.Text.RegularExpressions.Regex.Replace(sql, @"--.*$", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            cleanSql = System.Text.RegularExpressions.Regex.Replace(cleanSql, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);
            cleanSql = System.Text.RegularExpressions.Regex.Replace(cleanSql, @"\s+", " ").Trim();
            
            // Extract SELECT columns
            var selectMatch = System.Text.RegularExpressions.Regex.Match(
                cleanSql, 
                @"SELECT\s+(.*?)\s+FROM\s+([^\s;]+)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (!selectMatch.Success)
            {
                _logger.LogWarning("Fallback parser: Could not parse SELECT statement");
                return null;
            }
            
            var columnsText = selectMatch.Groups[1].Value;
            var tableText = selectMatch.Groups[2].Value;
            
            // Parse table name (handle database.schema.table format)
            var tableParts = tableText.Split('.');
            var table = new GSPTable
            {
                Name = tableParts[^1], // Last part is table name
                Schema = tableParts.Length > 1 ? tableParts[^2] : null,
                Database = tableParts.Length > 2 ? tableParts[^3] : null
            };
            
            // Parse columns (handle *, aliases, etc.)
            var result = new GSPLineageResult();
            
            if (columnsText.Trim() == "*")
            {
                _logger.LogInformation("Fallback parser: SELECT * detected - cannot determine columns without schema");
                return null;
            }
            
            // Split by comma (simple approach - doesn't handle functions with commas)
            var columnList = columnsText.Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
            
            foreach (var col in columnList)
            {
                // Handle aliases (e.g., "column_name AS alias" or "column_name alias")
                var colName = col;
                var asIndex = col.IndexOf(" AS ", StringComparison.OrdinalIgnoreCase);
                if (asIndex > 0)
                {
                    colName = col.Substring(0, asIndex).Trim();
                }
                else
                {
                    // Handle "column alias" without AS
                    var parts = col.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        colName = parts[0];
                    }
                }
                
                // Remove table prefix if present (e.g., "t.column_name" -> "column_name")
                if (colName.Contains('.'))
                {
                    colName = colName.Split('.')[^1];
                }
                
                // Clean up any remaining quotes or brackets
                colName = colName.Trim('[', ']', '"', '\'', '`');
                
                if (!string.IsNullOrEmpty(colName))
                {
                    result.ResultColumns.Add(new GSPColumn { Name = colName });
                    table.Columns.Add(colName);
                    
                    // Create lineage: source table column -> result column
                    result.ColumnLineages.Add(new GSPColumnLineage
                    {
                        SourceTable = table.Name,
                        SourceColumn = colName,
                        TargetColumn = colName
                    });
                }
            }
            
            result.SourceTables.Add(table);
            
            _logger.LogInformation("Fallback parser: Extracted {ColumnCount} columns from {TableName}", 
                result.ResultColumns.Count, table.Name);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback SQL parser failed");
            return null;
        }
    }

    private GSPLineageResult ParseGSPXmlResponse(string xmlContent)
    {
        var result = new GSPLineageResult();

        try
        {
            var doc = XDocument.Parse(xmlContent);
            var root = doc.Root;

            if (root == null)
            {
                _logger.LogWarning("GSP response has no root element");
                return result;
            }

            // Parse source tables
            var tables = root.Elements("table").ToList();
            foreach (var tableElement in tables)
            {
                var table = new GSPTable
                {
                    Name = tableElement.Attribute("name")?.Value ?? string.Empty,
                    Database = tableElement.Attribute("database")?.Value,
                    Schema = tableElement.Attribute("schema")?.Value
                };

                // Parse columns in this table
                var columns = tableElement.Elements("column").ToList();
                foreach (var columnElement in columns)
                {
                    var columnName = columnElement.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(columnName))
                    {
                        table.Columns.Add(columnName);
                    }
                }

                result.SourceTables.Add(table);
                _logger.LogDebug("Parsed table: {TableName} with {ColumnCount} columns", table.Name, table.Columns.Count);
            }

            // Parse result set columns
            var resultSets = root.Elements("resultset").ToList();
            foreach (var resultSetElement in resultSets)
            {
                var resultSetName = resultSetElement.Attribute("name")?.Value;
                var columns = resultSetElement.Elements("column").ToList();

                foreach (var columnElement in columns)
                {
                    var columnName = columnElement.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(columnName))
                    {
                        result.ResultColumns.Add(new GSPColumn
                        {
                            Name = columnName,
                            ParentName = resultSetName
                        });
                    }
                }

                _logger.LogDebug("Parsed result set: {ResultSetName} with {ColumnCount} columns", resultSetName, columns.Count);
            }

            // Parse column lineage relationships
            var relationships = root.Elements("relationship").ToList();
            foreach (var relationshipElement in relationships)
            {
                var effectType = relationshipElement.Attribute("effectType")?.Value;
                if (effectType != "select") continue;

                var sourceElement = relationshipElement.Element("source");
                var targetElement = relationshipElement.Element("target");

                if (sourceElement != null && targetElement != null)
                {
                    var sourceColumn = sourceElement.Attribute("column")?.Value;
                    var sourceParent = sourceElement.Attribute("parent_name")?.Value;
                    var targetColumn = targetElement.Attribute("column")?.Value;

                    if (!string.IsNullOrEmpty(sourceColumn) && !string.IsNullOrEmpty(targetColumn) && !string.IsNullOrEmpty(sourceParent))
                    {
                        result.ColumnLineages.Add(new GSPColumnLineage
                        {
                            SourceTable = sourceParent,
                            SourceColumn = sourceColumn,
                            TargetColumn = targetColumn
                        });
                    }
                }
            }

            _logger.LogInformation(
                "GSP parsing complete: {TableCount} tables, {ResultColumnCount} result columns, {LineageCount} lineages",
                result.SourceTables.Count,
                result.ResultColumns.Count,
                result.ColumnLineages.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse GSP XML response");
            return result;
        }
    }
}
