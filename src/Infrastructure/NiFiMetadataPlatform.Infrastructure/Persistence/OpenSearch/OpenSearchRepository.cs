using Microsoft.Extensions.Logging;
using OpenSearch.Client;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Entities;
using NiFiMetadataPlatform.Domain.Enums;
using NiFiMetadataPlatform.Domain.ValueObjects;
using NiFiMetadataPlatform.Infrastructure.Configuration;

namespace NiFiMetadataPlatform.Infrastructure.Persistence.OpenSearch;

/// <summary>
/// OpenSearch implementation of the search repository.
/// </summary>
public sealed class OpenSearchRepository : ISearchRepository
{
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchSettings _settings;
    private readonly ILogger<OpenSearchRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenSearchRepository"/> class.
    /// </summary>
    /// <param name="client">The OpenSearch client.</param>
    /// <param name="settings">The OpenSearch settings.</param>
    /// <param name="logger">The logger.</param>
    public OpenSearchRepository(
        IOpenSearchClient client,
        OpenSearchSettings settings,
        ILogger<OpenSearchRepository> logger)
    {
        _client = client;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result> IndexEntityAsync(
        NiFiProcessor processor,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = MapToDocument(processor);

            var response = await _client.IndexAsync(
                document,
                idx => idx.Index(_settings.IndexName).Id(processor.Fqn.Value),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Failed to index processor {Fqn}: {Error}",
                    processor.Fqn.Value,
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result.Failure($"Failed to index entity: {response.OriginalException?.Message}");
            }

            _logger.LogDebug("Indexed processor {Fqn}", processor.Fqn.Value);

            return Domain.Common.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing processor {Fqn}", processor.Fqn.Value);
            return Domain.Common.Result.Failure($"Failed to index entity: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result> UpdateEntityAsync(
        NiFiProcessor processor,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = MapToDocument(processor);

            var response = await _client.UpdateAsync<ProcessorDocument>(
                processor.Fqn.Value,
                u => u
                    .Index(_settings.IndexName)
                    .Doc(document)
                    .DocAsUpsert(true),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Failed to update processor {Fqn}: {Error}",
                    processor.Fqn.Value,
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result.Failure($"Failed to update entity: {response.OriginalException?.Message}");
            }

            _logger.LogDebug("Updated processor {Fqn}", processor.Fqn.Value);

            return Domain.Common.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating processor {Fqn}", processor.Fqn.Value);
            return Domain.Common.Result.Failure($"Failed to update entity: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result> DeleteEntityAsync(
        string fqn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.DeleteAsync<ProcessorDocument>(
                fqn,
                d => d.Index(_settings.IndexName),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Failed to delete processor {Fqn}: {Error}",
                    fqn,
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result.Failure($"Failed to delete entity: {response.OriginalException?.Message}");
            }

            _logger.LogDebug("Deleted processor {Fqn}", fqn);

            return Domain.Common.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting processor {Fqn}", fqn);
            return Domain.Common.Result.Failure($"Failed to delete entity: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result<NiFiProcessor?>> GetByFqnAsync(
        string fqn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetAsync<ProcessorDocument>(
                fqn,
                g => g.Index(_settings.IndexName),
                cancellationToken);

            if (!response.IsValid || !response.Found)
            {
                if (!response.Found)
                {
                    return Domain.Common.Result<NiFiProcessor?>.Success(null);
                }

                _logger.LogError(
                    "Failed to get processor {Fqn}: {Error}",
                    fqn,
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result<NiFiProcessor?>.Failure($"Failed to get entity: {response.OriginalException?.Message}");
            }

            var processor = MapToEntity(response.Source!);

            return Domain.Common.Result<NiFiProcessor?>.Success(processor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processor {Fqn}", fqn);
            return Domain.Common.Result<NiFiProcessor?>.Failure($"Failed to get entity: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result<List<NiFiProcessor>>> BulkGetAsync(
        List<string> fqns,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.MultiGetAsync(
                m => m
                    .Index(_settings.IndexName)
                    .GetMany<ProcessorDocument>(fqns),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Failed to bulk get processors: {Error}",
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result<List<NiFiProcessor>>.Failure($"Failed to bulk get entities: {response.OriginalException?.Message}");
            }

            var processors = new List<NiFiProcessor>();
            foreach (var hit in response.Hits.Where(h => h.Found && h.Source != null))
            {
                if (hit.Source is ProcessorDocument doc)
                {
                    processors.Add(MapToEntity(doc));
                }
            }

            _logger.LogDebug("Bulk retrieved {Count} processors", processors.Count);

            return Domain.Common.Result<List<NiFiProcessor>>.Success(processors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk getting processors");
            return Domain.Common.Result<List<NiFiProcessor>>.Failure($"Failed to bulk get entities: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result<List<NiFiProcessor>>> SearchAsync(
        string query,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.SearchAsync<ProcessorDocument>(
                s => s
                    .Index(_settings.IndexName)
                    .From(skip)
                    .Size(take)
                    .Query(q => q
                        .MultiMatch(m => m
                            .Query(query)
                            .Fields(f => f
                                .Field(p => p.Name)
                                .Field(p => p.Description)
                                .Field(p => p.Type)
                                .Field(p => p.Owner)))),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Failed to search processors: {Error}",
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result<List<NiFiProcessor>>.Failure($"Failed to search: {response.OriginalException?.Message}");
            }

            var processors = new List<NiFiProcessor>();
            if (response.Documents != null)
            {
                foreach (var doc in response.Documents)
                {
                    processors.Add(MapToEntity(doc));
                }
            }

            _logger.LogDebug("Search returned {Count} results", processors.Count);

            return Domain.Common.Result<List<NiFiProcessor>>.Success(processors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching processors with query '{Query}'", query);
            return Domain.Common.Result<List<NiFiProcessor>>.Failure($"Failed to search: {ex.Message}");
        }
    }

    private static ProcessorDocument MapToDocument(NiFiProcessor processor)
    {
        return new ProcessorDocument
        {
            Fqn = processor.Fqn.Value,
            Name = processor.Name.Value,
            Type = processor.Type.Value,
            Status = processor.Status.ToString(),
            Properties = processor.Properties.ToDictionary(),
            ParentProcessGroupId = processor.ParentProcessGroupId.Value,
            Description = processor.Description,
            Owner = processor.Owner,
            Tags = processor.Tags.ToList(),
            Platform = "NiFi",
            CreatedAt = processor.CreatedAt,
            UpdatedAt = processor.UpdatedAt
        };
    }

    private static NiFiProcessor MapToEntity(ProcessorDocument document)
    {
        var fqn = ProcessorFqn.Parse(document.Fqn);
        var name = ProcessorName.Create(document.Name);
        var type = ProcessorType.Parse(document.Type);
        var parentId = ProcessGroupId.Parse(document.ParentProcessGroupId);

        var processor = NiFiProcessor.Create(fqn, name, type, parentId);

        if (document.Properties.Count > 0)
        {
            var properties = ProcessorProperties.Create(document.Properties);
            processor.UpdateProperties(properties);
        }

        if (!string.IsNullOrWhiteSpace(document.Description))
        {
            processor.UpdateDescription(document.Description);
        }

        if (!string.IsNullOrWhiteSpace(document.Owner))
        {
            processor.SetOwner(document.Owner);
        }

        foreach (var tag in document.Tags)
        {
            processor.AddTag(tag);
        }

        if (Enum.TryParse<ProcessorStatus>(document.Status, out var status))
        {
            if (status == ProcessorStatus.Inactive)
            {
                processor.Deactivate();
            }
            else if (status == ProcessorStatus.Deleted)
            {
                processor.Delete();
            }
        }

        processor.ClearDomainEvents();

        return processor;
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result<(List<NiFiProcessor> Processors, int Total)>> SearchWithFiltersAsync(
        string query,
        string? typeName,
        string? platform,
        int count,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchQuery = query == "*" || string.IsNullOrWhiteSpace(query) ? "*" : query;

            var mustQueries = new List<QueryContainer>();

            if (searchQuery != "*")
            {
                mustQueries.Add(new MultiMatchQuery
                {
                    Query = searchQuery,
                    Fields = new[] { "name^2", "type", "description", "properties.*" }
                });
            }
            else
            {
                mustQueries.Add(new MatchAllQuery());
            }

            var filterQueries = new List<QueryContainer>();

            if (!string.IsNullOrWhiteSpace(platform))
            {
                filterQueries.Add(new TermQuery { Field = "platform.keyword", Value = platform });
            }

            if (!string.IsNullOrWhiteSpace(typeName))
            {
                filterQueries.Add(new TermQuery { Field = "type.keyword", Value = typeName });
            }

            // If no type filter and platform is NiFi, only return processor documents (not tables, schemas, columns)
            if (string.IsNullOrWhiteSpace(typeName) && platform?.Equals("NiFi", StringComparison.OrdinalIgnoreCase) == true)
            {
                filterQueries.Add(new WildcardQuery { Field = "fqn.keyword", Value = "nifi://container/*/processor/*" });
            }

            var searchRequest = new SearchRequest(_settings.IndexName)
            {
                Query = new BoolQuery
                {
                    Must = mustQueries,
                    Filter = filterQueries
                },
                Size = count,
                Sort = new List<ISort>
                {
                    new FieldSort { Field = "_score", Order = SortOrder.Descending },
                    new FieldSort { Field = "name.keyword", Order = SortOrder.Ascending }
                }
            };

            var response = await _client.SearchAsync<ProcessorDocument>(searchRequest, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Search with filters failed: {Error}",
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result<(List<NiFiProcessor>, int)>.Failure(
                    $"Search failed: {response.OriginalException?.Message}");
            }

            // Map documents to entities
            // Filter out columns if no type filter is specified (wildcard can't exclude /column/ suffix)
            var processors = response.Documents
                .Where(doc => doc.Fqn != null && 
                             (string.IsNullOrWhiteSpace(typeName) ? !doc.Fqn.Contains("/column/") : true))
                .Select(MapToEntity)
                .ToList();

            var total = (int)(response.Total > int.MaxValue ? int.MaxValue : response.Total);

            _logger.LogDebug("Search with filters returned {Count} results out of {Total}", processors.Count, total);

            return Domain.Common.Result<(List<NiFiProcessor>, int)>.Success((processors, total));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching with filters");
            return Domain.Common.Result<(List<NiFiProcessor>, int)>.Failure($"Search failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result<(List<Application.DTOs.AtlasEntityDto> Entities, int Total)>> SearchAllEntitiesAsync(
        string query,
        string? typeName,
        string? platform,
        int count,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchQuery = query == "*" || string.IsNullOrWhiteSpace(query) ? "*" : query;
            var mustQueries = new List<QueryContainer>();

            if (searchQuery != "*")
            {
                mustQueries.Add(new MultiMatchQuery
                {
                    Query = searchQuery,
                    Fields = new[] { "name^3", "type", "description", "fqn" }
                });
            }
            else
            {
                mustQueries.Add(new MatchAllQuery());
            }

            var filterQueries = new List<QueryContainer>();

            if (!string.IsNullOrWhiteSpace(platform))
            {
                filterQueries.Add(new TermQuery { Field = "platform.keyword", Value = platform });
            }

            if (!string.IsNullOrWhiteSpace(typeName))
            {
                filterQueries.Add(new TermQuery { Field = "type.keyword", Value = typeName });
            }

            // Exclude raw column entries from general search (too noisy)
            if (string.IsNullOrWhiteSpace(typeName))
            {
                filterQueries.Add(new BoolQuery
                {
                    MustNot = new List<QueryContainer>
                    {
                        new TermQuery { Field = "type.keyword", Value = "COLUMN" }
                    }
                });
            }

            var searchRequest = new SearchRequest(_settings.IndexName)
            {
                Query = new BoolQuery { Must = mustQueries, Filter = filterQueries },
                Size = count,
                Sort = new List<ISort>
                {
                    new FieldSort { Field = "_score", Order = SortOrder.Descending },
                    new FieldSort { Field = "name.keyword", Order = SortOrder.Ascending }
                }
            };

            var response = await _client.SearchAsync<ProcessorDocument>(searchRequest, cancellationToken);

            if (!response.IsValid)
            {
                return Domain.Common.Result<(List<Application.DTOs.AtlasEntityDto>, int)>.Failure(
                    $"Search failed: {response.OriginalException?.Message}");
            }

            var entities = response.Documents
                .Where(doc => doc.Fqn != null)
                .Select(doc => new Application.DTOs.AtlasEntityDto
                {
                    Urn = doc.Fqn,
                    Type = doc.Type ?? "DATASET",
                    Name = doc.Name ?? doc.Fqn.Split('/').LastOrDefault() ?? doc.Fqn,
                    Platform = doc.Platform ?? "Unknown",
                    Description = doc.Description ?? string.Empty,
                    Properties = doc.Properties ?? new Dictionary<string, string>(),
                    ParentContainerUrn = doc.ParentProcessGroupId
                })
                .ToList();

            var total = (int)(response.Total > int.MaxValue ? int.MaxValue : response.Total);
            return Domain.Common.Result<(List<Application.DTOs.AtlasEntityDto>, int)>.Success((entities, total));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SearchAllEntitiesAsync");
            return Domain.Common.Result<(List<Application.DTOs.AtlasEntityDto>, int)>.Failure($"Search failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result<List<NiFiProcessor>>> GetHierarchyAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchRequest = new SearchRequest(_settings.IndexName)
            {
                Query = new MatchAllQuery(),
                Size = 1000,
                Sort = new List<ISort>
                {
                    new FieldSort { Field = "name.keyword", Order = SortOrder.Ascending }
                }
            };

            var response = await _client.SearchAsync<ProcessorDocument>(searchRequest, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Get hierarchy failed: {Error}",
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result<List<NiFiProcessor>>.Failure(
                    $"Get hierarchy failed: {response.OriginalException?.Message}");
            }

            var processors = response.Documents
                .Select(MapToEntity)
                .ToList();

            _logger.LogDebug("Get hierarchy returned {Count} results", processors.Count);

            return Domain.Common.Result<List<NiFiProcessor>>.Success(processors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hierarchy");
            return Domain.Common.Result<List<NiFiProcessor>>.Failure($"Get hierarchy failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result<Dictionary<string, int>>> GetPlatformStatsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For NiFi: count only processors (not containers/process-groups/columns)
            // For JDBC platforms (MSSQL, Snowflake, etc.): count only TABLE entities
            // We run two queries and merge results.

            var platformStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // --- Query 1: NiFi processors only (exclude CONTAINER, COLUMN, SCHEMA, DATABASE) ---
            var nifiRequest = new SearchRequest(_settings.IndexName)
            {
                Query = new BoolQuery
                {
                    Must = new List<QueryContainer>
                    {
                        new TermQuery { Field = "platform.keyword", Value = "NiFi" }
                    },
                    MustNot = new List<QueryContainer>
                    {
                        new TermQuery { Field = "type.keyword", Value = "CONTAINER" },
                        new TermQuery { Field = "type.keyword", Value = "COLUMN" },
                        new TermQuery { Field = "type.keyword", Value = "SCHEMA" },
                        new TermQuery { Field = "type.keyword", Value = "DATABASE" }
                    }
                },
                Size = 0,
                Aggregations = new AggregationDictionary
                {
                    { "platforms", new TermsAggregation("platforms") { Field = "platform.keyword", Size = 100 } }
                }
            };

            var nifiResponse = await _client.SearchAsync<ProcessorDocument>(nifiRequest, cancellationToken);
            if (nifiResponse.IsValid && nifiResponse.Total > 0)
                platformStats["NiFi"] = (int)nifiResponse.Total;

            // --- Query 2: JDBC TABLE entities only (MSSQL, Snowflake, etc.) ---
            var tableRequest = new SearchRequest(_settings.IndexName)
            {
                Query = new BoolQuery
                {
                    Must = new List<QueryContainer>
                    {
                        new TermQuery { Field = "type.keyword", Value = "TABLE" }
                    },
                    MustNot = new List<QueryContainer>
                    {
                        new TermQuery { Field = "platform.keyword", Value = "NiFi" }
                    }
                },
                Size = 0,
                Aggregations = new AggregationDictionary
                {
                    { "platforms", new TermsAggregation("platforms") { Field = "platform.keyword", Size = 100 } }
                }
            };

            var tableResponse = await _client.SearchAsync<ProcessorDocument>(tableRequest, cancellationToken);
            if (tableResponse.IsValid &&
                tableResponse.Aggregations.TryGetValue("platforms", out var tableAgg) &&
                tableAgg is BucketAggregate tableBucket)
            {
                foreach (var bucket in tableBucket.Items.OfType<KeyedBucket<object>>())
                {
                    var platform = bucket.Key.ToString() ?? "Unknown";
                    var count = (int)(bucket.DocCount ?? 0);
                    platformStats[platform] = count;
                }
            }

            _logger.LogDebug("Get platform stats returned {Count} platforms", platformStats.Count);
            return Domain.Common.Result<Dictionary<string, int>>.Success(platformStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform stats");
            return Domain.Common.Result<Dictionary<string, int>>.Failure($"Get platform stats failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result<List<NiFiProcessor>>> GetChildrenAsync(
        string parentFqn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchRequest = new SearchRequest(_settings.IndexName)
            {
                Query = new TermQuery
                {
                    Field = "parentProcessGroupId.keyword",
                    Value = parentFqn
                },
                Size = 1000,
                Sort = new List<ISort>
                {
                    new FieldSort { Field = "name.keyword", Order = SortOrder.Ascending }
                }
            };

            var response = await _client.SearchAsync<ProcessorDocument>(searchRequest, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Get children failed for {ParentFqn}: {Error}",
                    parentFqn,
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result<List<NiFiProcessor>>.Failure(
                    $"Get children failed: {response.OriginalException?.Message}");
            }

            var processors = response.Documents
                .Select(MapToEntity)
                .ToList();

            _logger.LogDebug("Get children for {ParentFqn} returned {Count} results", parentFqn, processors.Count);

            return Domain.Common.Result<List<NiFiProcessor>>.Success(processors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting children for {ParentFqn}", parentFqn);
            return Domain.Common.Result<List<NiFiProcessor>>.Failure($"Get children failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result> IndexColumnAsync(
        object column,
        string fqn,
        string entityType,
        string platform,
        string parentFqn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = new
            {
                fqn = fqn,
                type = entityType,
                platform = platform,
                parentContainerUrn = parentFqn,
                column = column,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            };

            var response = await _client.IndexAsync(
                document,
                idx => idx.Index(_settings.IndexName).Id(fqn),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Failed to index column {Fqn}: {Error}",
                    fqn,
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result.Failure($"Failed to index column: {response.OriginalException?.Message}");
            }

            _logger.LogDebug("Indexed column {Fqn}", fqn);

            return Domain.Common.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing column {Fqn}", fqn);
            return Domain.Common.Result.Failure($"Failed to index column: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result> IndexTableAsync(
        DatabaseTable table,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = new
            {
                fqn = table.Fqn.Value,
                type = "TABLE",
                name = table.Name,
                platform = table.Platform,
                parentContainerUrn = table.Fqn.GetParentFqn()?.Value,
                properties = new Dictionary<string, string>
                {
                    ["server"] = table.Server,
                    ["database"] = table.Database,
                    ["schema"] = table.Schema,
                    ["tableType"] = table.TableType ?? "TABLE"
                },
                description = table.Description,
                createdAt = table.CreatedAt,
                updatedAt = table.UpdatedAt
            };

            var response = await _client.IndexAsync(
                document,
                idx => idx.Index(_settings.IndexName).Id(table.Fqn.Value),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Failed to index table {Fqn}: {Error}",
                    table.Fqn.Value,
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result.Failure($"Failed to index table: {response.OriginalException?.Message}");
            }

            _logger.LogDebug("Indexed table {Fqn}", table.Fqn.Value);

            return Domain.Common.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing table {Fqn}", table.Fqn.Value);
            return Domain.Common.Result.Failure($"Failed to index table: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result> IndexDatabaseEntityAsync(
        string fqn,
        string name,
        string entityType,
        string platform,
        string? parentFqn,
        Dictionary<string, string> properties,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = new
            {
                fqn = fqn,
                type = entityType,
                name = name,
                platform = platform,
                parentContainerUrn = parentFqn,
                properties = properties,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            };

            var response = await _client.IndexAsync(
                document,
                idx => idx.Index(_settings.IndexName).Id(fqn),
                cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Failed to index database entity {Fqn}: {Error}",
                    fqn,
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result.Failure($"Failed to index database entity: {response.OriginalException?.Message}");
            }

            _logger.LogDebug("Indexed database entity {Fqn} of type {Type}", fqn, entityType);

            return Domain.Common.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing database entity {Fqn}", fqn);
            return Domain.Common.Result.Failure($"Failed to index database entity: {ex.Message}");
        }
    }

    public async Task<Domain.Common.Result<List<string>>> GetColumnUrnsByProcessorFqnAsync(
        string processorFqn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchRequest = new SearchRequest(_settings.IndexName)
            {
                Query = new BoolQuery
                {
                    Filter = new List<QueryContainer>
                    {
                        new PrefixQuery { Field = "fqn.keyword", Value = $"{processorFqn}/column/" },
                        new TermQuery { Field = "type.keyword", Value = "COLUMN" }
                    }
                },
                Size = 1000,
                Source = new SourceFilter { Includes = new[] { "fqn" } }
            };

            var response = await _client.SearchAsync<ProcessorDocument>(searchRequest, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Failed to get columns for processor {ProcessorFqn}: {Error}",
                    processorFqn,
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result<List<string>>.Failure(
                    $"Search failed: {response.OriginalException?.Message}");
            }

            var columnUrns = response.Documents
                .Where(doc => !string.IsNullOrWhiteSpace(doc.Fqn))
                .Select(doc => doc.Fqn)
                .ToList();

            _logger.LogDebug("Found {Count} columns for processor {ProcessorFqn}", columnUrns.Count, processorFqn);

            return Domain.Common.Result<List<string>>.Success(columnUrns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting columns for processor {ProcessorFqn}", processorFqn);
            return Domain.Common.Result<List<string>>.Failure($"Failed to get columns: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result<List<Application.DTOs.ColumnInfoDto>>> GetColumnsByTableFqnAsync(
        string tableFqn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchRequest = new SearchRequest(_settings.IndexName)
            {
                Query = new BoolQuery
                {
                    Filter = new List<QueryContainer>
                    {
                        new TermQuery { Field = "parentContainerUrn.keyword", Value = tableFqn },
                        new TermQuery { Field = "type.keyword", Value = "COLUMN" }
                    }
                },
                Size = 1000
            };

            var response = await _client.SearchAsync<ProcessorDocument>(searchRequest, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError(
                    "Failed to get columns for table {TableFqn}: {Error}",
                    tableFqn,
                    response.OriginalException?.Message ?? response.ServerError?.ToString());
                return Domain.Common.Result<List<Application.DTOs.ColumnInfoDto>>.Failure(
                    $"Search failed: {response.OriginalException?.Message}");
            }

            var columns = response.Documents
                .Where(doc => !string.IsNullOrWhiteSpace(doc.Fqn))
                .Select(doc =>
                {
                    // Extract column name from FQN: .../column/{name}
                    var name = doc.Name;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        var idx = doc.Fqn.LastIndexOf("/column/", StringComparison.OrdinalIgnoreCase);
                        name = idx >= 0 ? doc.Fqn[(idx + 8)..] : doc.Fqn.Split('/').Last();
                    }

                    doc.Properties.TryGetValue("dataType", out var dataType);
                    doc.Properties.TryGetValue("nativeType", out var nativeType);

                    return new Application.DTOs.ColumnInfoDto
                    {
                        Urn = doc.Fqn,
                        Name = name,
                        DataType = dataType ?? "VARCHAR",
                        NativeType = nativeType ?? string.Empty,
                        Description = doc.Description ?? string.Empty
                    };
                })
                .OrderBy(c => c.Name)
                .ToList();

            _logger.LogDebug("Found {Count} columns for table {TableFqn}", columns.Count, tableFqn);

            return Domain.Common.Result<List<Application.DTOs.ColumnInfoDto>>.Success(columns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting columns for table {TableFqn}", tableFqn);
            return Domain.Common.Result<List<Application.DTOs.ColumnInfoDto>>.Failure($"Failed to get columns: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Domain.Common.Result<Application.DTOs.AtlasEntityDto?>> GetRawEntityByFqnAsync(
        string fqn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Search by fqn.keyword - works for any entity type (processor, table, column, etc.)
            var searchRequest = new SearchRequest(_settings.IndexName)
            {
                Query = new TermQuery { Field = "fqn.keyword", Value = fqn },
                Size = 1
            };

            var response = await _client.SearchAsync<ProcessorDocument>(searchRequest, cancellationToken);

            if (!response.IsValid || !response.Documents.Any())
            {
                _logger.LogDebug("Entity not found by FQN: {Fqn}", fqn);
                return Domain.Common.Result<Application.DTOs.AtlasEntityDto?>.Success(null);
            }

            var doc = response.Documents.First();

            // Build properties — start with stored properties, then enrich with column data
            var props = new Dictionary<string, string>(doc.Properties ?? new Dictionary<string, string>());

            // Determine name: columns store it in Column.Name, processors in Name
            var name = doc.Name;
            var parentUrn = string.IsNullOrEmpty(doc.ParentProcessGroupId) ? doc.ParentContainerUrn : doc.ParentProcessGroupId;

            if (doc.Column != null)
            {
                // Column entity — extract all column metadata into properties
                if (!string.IsNullOrEmpty(doc.Column.Name)) name = doc.Column.Name;

                var tableFqn = doc.Column.TableFqn?.Value ?? doc.Column.ProcessorFqn?.Value ?? parentUrn ?? string.Empty;
                if (!string.IsNullOrEmpty(tableFqn)) props["parentFqn"] = tableFqn;

                // Parse server/db/schema/table from the table FQN (jdbc://server/db/schema/table)
                if (!string.IsNullOrEmpty(tableFqn) && tableFqn.StartsWith("jdbc://"))
                {
                    var parts = tableFqn.Replace("jdbc://", string.Empty).Split('/');
                    if (parts.Length >= 1) props["server"] = parts[0];
                    if (parts.Length >= 2) props["database"] = parts[1];
                    if (parts.Length >= 3) props["schema"] = parts[2];
                    if (parts.Length >= 4) props["table"] = parts[3];
                }

                if (!string.IsNullOrEmpty(doc.Column.DataType)) props["dataType"] = doc.Column.DataType;
                if (!string.IsNullOrEmpty(doc.Column.NativeType)) props["nativeType"] = doc.Column.NativeType;
                if (doc.Column.IsNullable.HasValue) props["nullable"] = doc.Column.IsNullable.Value ? "Yes" : "No";
                if (doc.Column.IsPrimaryKey.HasValue) props["primaryKey"] = doc.Column.IsPrimaryKey.Value ? "Yes" : "No";
                if (doc.Column.OrdinalPosition.HasValue) props["ordinalPosition"] = doc.Column.OrdinalPosition.Value.ToString();
                if (doc.Column.Precision.HasValue) props["precision"] = doc.Column.Precision.Value.ToString();
                if (doc.Column.Scale.HasValue) props["scale"] = doc.Column.Scale.Value.ToString();
                if (!string.IsNullOrEmpty(doc.Column.Description)) props["description"] = doc.Column.Description;
            }

            var dto = new Application.DTOs.AtlasEntityDto
            {
                Urn = doc.Fqn,
                Type = doc.Type ?? "DATASET",
                Name = name,
                Platform = doc.Platform ?? "Unknown",
                Description = doc.Description ?? string.Empty,
                Properties = props,
                ParentContainerUrn = parentUrn
            };

            return Domain.Common.Result<Application.DTOs.AtlasEntityDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting raw entity by FQN: {Fqn}", fqn);
            return Domain.Common.Result<Application.DTOs.AtlasEntityDto?>.Failure($"Failed to get entity: {ex.Message}");
        }
    }
}
