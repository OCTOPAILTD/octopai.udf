using Microsoft.Extensions.Logging;
using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.DTOs;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Entities;
using NiFiMetadataPlatform.Domain.Enums;

namespace NiFiMetadataPlatform.Application.Queries.Handlers;

/// <summary>
/// Handler for GetAtlasLineageQuery.
/// </summary>
public sealed class GetAtlasLineageQueryHandler : IQueryHandler<GetAtlasLineageQuery, Result<AtlasLineageResponse>>
{
    private readonly IGraphRepository _graphRepository;
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<GetAtlasLineageQueryHandler> _logger;

    public GetAtlasLineageQueryHandler(
        IGraphRepository graphRepository,
        ISearchRepository searchRepository,
        ILogger<GetAtlasLineageQueryHandler> logger)
    {
        _graphRepository = graphRepository;
        _searchRepository = searchRepository;
        _logger = logger;
    }

    public async Task<Result<AtlasLineageResponse>> Handle(
        GetAtlasLineageQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting Atlas lineage for {Urn}, direction={Direction}, maxHops={MaxHops}",
            request.Urn,
            request.Direction,
            request.MaxHops);

        var isColumnUrn = request.Urn.Contains("/column/", StringComparison.OrdinalIgnoreCase);

        if (isColumnUrn)
        {
            return await HandleColumnLineageAsync(request, cancellationToken);
        }

        return await HandleProcessorLineageAsync(request, cancellationToken);
    }

    /// <summary>
    /// For a column URN: BFS-traverse the column lineage graph and return
    /// parent-entity nodes with ColumnLineageDto edges (entity-to-entity, column-annotated).
    /// This produces the same diagram shape as processor lineage but filtered to one column.
    /// </summary>
    private async Task<Result<AtlasLineageResponse>> HandleColumnLineageAsync(
        GetAtlasLineageQuery request,
        CancellationToken cancellationToken)
    {
        var direction = ParseDirection(request.Direction);
        var columnName = ExtractColumnName(request.Urn);
        var centerEntityUrn = ExtractParentUrn(request.Urn);

        // BFS over column edges to collect all column URNs in the chain
        var visitedColumnUrns = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { request.Urn };
        var frontier = new Queue<string>();
        frontier.Enqueue(request.Urn);

        // All directed edges between column URNs
        var columnEdges = new List<(string From, string To)>();

        var maxHops = Math.Max(Math.Min(request.MaxHops, 10), 8);

        for (var hop = 0; hop < maxHops && frontier.Count > 0; hop++)
        {
            var batch = new List<string>();
            while (frontier.Count > 0) batch.Add(frontier.Dequeue());

            var edgesResult = await _graphRepository.GetDirectColumnEdgesAsync(batch, cancellationToken);
            if (edgesResult.IsFailure || edgesResult.Value == null) continue;

            foreach (var (from, to) in edgesResult.Value)
            {
                columnEdges.Add((from, to));

                if (direction is LineageDirection.Downstream or LineageDirection.Both)
                {
                    if (batch.Contains(from) && !visitedColumnUrns.Contains(to))
                    {
                        visitedColumnUrns.Add(to);
                        frontier.Enqueue(to);
                    }
                }

                if (direction is LineageDirection.Upstream or LineageDirection.Both)
                {
                    if (batch.Contains(to) && !visitedColumnUrns.Contains(from))
                    {
                        visitedColumnUrns.Add(from);
                        frontier.Enqueue(from);
                    }
                }
            }
        }

        // Build entity-level ColumnLineageDtos from column edges
        var columnLineageDtos = new List<ColumnLineageDto>();
        var edgeSet = new HashSet<string>();

        foreach (var (fromColUrn, toColUrn) in columnEdges)
        {
            var fromEntityUrn = ExtractParentUrn(fromColUrn);
            var toEntityUrn = ExtractParentUrn(toColUrn);
            if (fromEntityUrn == toEntityUrn) continue;

            var edgeKey = $"{fromEntityUrn}→{toEntityUrn}";
            if (!edgeSet.Add(edgeKey)) continue;

            columnLineageDtos.Add(new ColumnLineageDto
            {
                FromUrn = fromEntityUrn,
                FromColumn = ExtractColumnName(fromColUrn),
                ToUrn = toEntityUrn,
                ToColumn = ExtractColumnName(toColUrn),
            });
        }

        // Determine upstream vs downstream entity URNs relative to the center entity
        var upstreamEntityUrns = new HashSet<string>();
        var downstreamEntityUrns = new HashSet<string>();

        foreach (var dto in columnLineageDtos)
        {
            if (dto.ToUrn == centerEntityUrn) upstreamEntityUrns.Add(dto.FromUrn);
            else if (dto.FromUrn == centerEntityUrn) downstreamEntityUrns.Add(dto.ToUrn);
            else
            {
                // Determine side by checking if center is reachable upstream or downstream
                downstreamEntityUrns.Add(dto.ToUrn);
                upstreamEntityUrns.Add(dto.FromUrn);
            }
        }

        // Remove center entity from up/downstream sets
        upstreamEntityUrns.Remove(centerEntityUrn);
        downstreamEntityUrns.Remove(centerEntityUrn);

        // Enrich entity URNs with metadata from OpenSearch
        async Task<AtlasEntityDto> EnrichEntityAsync(string entityUrn)
        {
            var r = await _searchRepository.GetRawEntityByFqnAsync(entityUrn, cancellationToken);
            if (r.IsSuccess && r.Value != null)
            {
                return r.Value;
            }

            return CreateEntityFromUrn(entityUrn);
        }

        var upstreamEntities = await Task.WhenAll(upstreamEntityUrns.Select(EnrichEntityAsync));
        var downstreamEntities = await Task.WhenAll(downstreamEntityUrns.Select(EnrichEntityAsync));

        _logger.LogInformation(
            "Column lineage for {Column}: {UpCount} upstream entities, {DownCount} downstream entities, {EdgeCount} edges",
            columnName, upstreamEntities.Length, downstreamEntities.Length, columnLineageDtos.Count);

        return Result<AtlasLineageResponse>.Success(new AtlasLineageResponse
        {
            Upstream = upstreamEntities.ToList(),
            Downstream = downstreamEntities.ToList(),
            ColumnLineage = columnLineageDtos,
        });
    }

    /// <summary>
    /// For a processor/table URN: group columns by their parent entity and return
    /// processor-level nodes with column lineage details.
    /// Does multi-hop traversal so the full upstream/downstream chain is visible.
    /// </summary>
    private async Task<Result<AtlasLineageResponse>> HandleProcessorLineageAsync(
        GetAtlasLineageQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching processor-level lineage for {Urn}", request.Urn);

        var direction = ParseDirection(request.Direction);
        // All column lineage edges (from → to at entity level), deduped
        var entityEdgeSet = new HashSet<string>();
        var columnLineageDtos = new List<ColumnLineageDto>();
        var allEntityUrns = new HashSet<string>();

        // Collect all entity URNs reachable by traversing column edges hop-by-hop
        // Start from the center entity and walk outward
        var visitedEntities = new HashSet<string> { request.Urn };
        var frontier = new Queue<string>();
        frontier.Enqueue(request.Urn);

        var maxHops = Math.Max(Math.Min(request.MaxHops, 10), 5); // at least 5 hops to traverse full chains

        // Cache of entity → column names (extracted from URNs)
        var entityColumnNamesCache = new Dictionary<string, List<string>>();

        async Task<List<string>> GetEntityColumnNamesAsync(string entityUrn)
        {
            if (entityColumnNamesCache.TryGetValue(entityUrn, out var cached)) return cached;
            var r = await _searchRepository.GetColumnUrnsByProcessorFqnAsync(entityUrn, cancellationToken);
            var names = (r.IsSuccess && r.Value != null)
                ? r.Value.Select(ExtractColumnName).Distinct().ToList()
                : new List<string>();
            entityColumnNamesCache[entityUrn] = names;
            return names;
        }

        for (var hop = 0; hop < maxHops && frontier.Count > 0; hop++)
        {
            var currentBatch = new List<string>();
            while (frontier.Count > 0)
                currentBatch.Add(frontier.Dequeue());

            foreach (var entityUrn in currentBatch)
            {
                // Get columns for this entity
                var columnUrnsResult = await _searchRepository.GetColumnUrnsByProcessorFqnAsync(entityUrn, cancellationToken);
                if (columnUrnsResult.IsFailure || columnUrnsResult.Value == null || columnUrnsResult.Value.Count == 0)
                    continue;

                var myColumnUrns = columnUrnsResult.Value;
                var myColumnUrnSet = new HashSet<string>(myColumnUrns);
                entityColumnNamesCache[entityUrn] = myColumnUrns.Select(ExtractColumnName).Distinct().ToList();

                // Get direct edges for these columns
                var edgesResult = await _graphRepository.GetDirectColumnEdgesAsync(myColumnUrns, cancellationToken);
                if (edgesResult.IsFailure || edgesResult.Value == null)
                    continue;

                // Collect connected entity URNs from the edges
                var connectedDownstream = new HashSet<string>();
                var connectedUpstream = new HashSet<string>();

                foreach (var (from, to) in edgesResult.Value)
                {
                    if (myColumnUrnSet.Contains(from) && !myColumnUrnSet.Contains(to))
                    {
                        var downstreamParent = ExtractParentUrn(to);
                        if (downstreamParent != entityUrn)
                        {
                            connectedDownstream.Add(downstreamParent);
                            allEntityUrns.Add(downstreamParent);
                            if (!visitedEntities.Contains(downstreamParent) && (direction is LineageDirection.Downstream or LineageDirection.Both))
                            {
                                visitedEntities.Add(downstreamParent);
                                frontier.Enqueue(downstreamParent);
                            }
                        }
                    }
                    else if (!myColumnUrnSet.Contains(from) && myColumnUrnSet.Contains(to))
                    {
                        var upstreamParent = ExtractParentUrn(from);
                        if (upstreamParent != entityUrn)
                        {
                            connectedUpstream.Add(upstreamParent);
                            allEntityUrns.Add(upstreamParent);
                            if (!visitedEntities.Contains(upstreamParent) && (direction is LineageDirection.Upstream or LineageDirection.Both))
                            {
                                visitedEntities.Add(upstreamParent);
                                frontier.Enqueue(upstreamParent);
                            }
                        }
                    }
                }

                // For each connected entity pair, emit one ColumnLineageDto per shared column
                foreach (var downstreamUrn in connectedDownstream)
                {
                    var edgeKey = $"{entityUrn}→{downstreamUrn}";
                    if (!entityEdgeSet.Add(edgeKey)) continue;

                    var myColNames = entityColumnNamesCache[entityUrn];
                    var theirColNames = await GetEntityColumnNamesAsync(downstreamUrn);
                    var sharedCols = myColNames.Intersect(theirColNames, StringComparer.OrdinalIgnoreCase).ToList();

                    // Fall back to all source columns if no intersection (pass-through)
                    if (sharedCols.Count == 0) sharedCols = myColNames;

                    foreach (var col in sharedCols)
                    {
                        columnLineageDtos.Add(new ColumnLineageDto
                        {
                            FromUrn = entityUrn,
                            FromColumn = col,
                            ToUrn = downstreamUrn,
                            ToColumn = col
                        });
                    }
                }

                foreach (var upstreamUrn in connectedUpstream)
                {
                    var edgeKey = $"{upstreamUrn}→{entityUrn}";
                    if (!entityEdgeSet.Add(edgeKey)) continue;

                    var myColNames = entityColumnNamesCache[entityUrn];
                    var theirColNames = await GetEntityColumnNamesAsync(upstreamUrn);
                    var sharedCols = theirColNames.Intersect(myColNames, StringComparer.OrdinalIgnoreCase).ToList();

                    if (sharedCols.Count == 0) sharedCols = theirColNames;

                    foreach (var col in sharedCols)
                    {
                        columnLineageDtos.Add(new ColumnLineageDto
                        {
                            FromUrn = upstreamUrn,
                            FromColumn = col,
                            ToUrn = entityUrn,
                            ToColumn = col
                        });
                    }
                }
            }
        }

        // Classify entities as upstream or downstream relative to the center
        // An entity is upstream if there's a path from it to the center
        // An entity is downstream if there's a path from the center to it
        var upstreamEntityUrns = new HashSet<string>();
        var downstreamEntityUrns = new HashSet<string>();

        foreach (var entityUrn in allEntityUrns)
        {
            // Check if this entity has an edge TO the center (directly or transitively)
            var isUpstream = columnLineageDtos.Any(e => e.ToUrn == request.Urn && e.FromUrn == entityUrn)
                || columnLineageDtos.Any(e => e.ToUrn == request.Urn
                    && columnLineageDtos.Any(e2 => e2.ToUrn == e.FromUrn && e2.FromUrn == entityUrn));

            // Check if the center has an edge TO this entity
            var isDownstream = columnLineageDtos.Any(e => e.FromUrn == request.Urn && e.ToUrn == entityUrn)
                || columnLineageDtos.Any(e => e.FromUrn == request.Urn
                    && columnLineageDtos.Any(e2 => e2.FromUrn == e.ToUrn && e2.ToUrn == entityUrn));

            if (isUpstream) upstreamEntityUrns.Add(entityUrn);
            if (isDownstream) downstreamEntityUrns.Add(entityUrn);
            if (!isUpstream && !isDownstream) upstreamEntityUrns.Add(entityUrn); // default to upstream
        }

        _logger.LogInformation(
            "Processor lineage for {Urn}: {UpCount} upstream entities, {DownCount} downstream entities, {ColCount} column mappings",
            request.Urn, upstreamEntityUrns.Count, downstreamEntityUrns.Count, columnLineageDtos.Count);

        // Build upstream/downstream entity DTOs by looking up names from OpenSearch
        var upstreamEntities = await BuildEntityDtosAsync(upstreamEntityUrns, cancellationToken);
        var downstreamEntities = await BuildEntityDtosAsync(downstreamEntityUrns, cancellationToken);

        return Result<AtlasLineageResponse>.Success(new AtlasLineageResponse
        {
            Upstream = upstreamEntities,
            Downstream = downstreamEntities,
            ColumnLineage = columnLineageDtos
        });
    }

    private async Task<List<AtlasEntityDto>> BuildEntityDtosAsync(
        HashSet<string> entityUrns,
        CancellationToken cancellationToken)
    {
        var entities = new List<AtlasEntityDto>();
        foreach (var urn in entityUrns)
        {
            // Try generic lookup first (works for TABLE, COLUMN, processor)
            var rawResult = await _searchRepository.GetRawEntityByFqnAsync(urn, cancellationToken);
            if (rawResult.IsSuccess && rawResult.Value != null)
            {
                entities.Add(rawResult.Value);
                continue;
            }

            // Try processor-specific lookup
            var entityResult = await _searchRepository.GetByFqnAsync(urn, cancellationToken);
            if (entityResult.IsSuccess && entityResult.Value != null)
            {
                entities.Add(new AtlasEntityDto
                {
                    Urn = entityResult.Value.Fqn.Value,
                    Type = "DATASET",
                    Name = entityResult.Value.Name.Value,
                    Platform = "NiFi",
                    Description = entityResult.Value.Description ?? string.Empty,
                    Properties = new Dictionary<string, string>(),
                    ParentContainerUrn = null
                });
            }
            else
            {
                // Fallback: derive name from URN
                entities.Add(CreateEntityFromUrn(urn));
            }
        }

        return entities;
    }

    /// <summary>
    /// Extracts the parent processor/table URN from a column URN.
    /// e.g. nifi://container/X/processor/Y/column/Z  →  nifi://container/X/processor/Y
    /// e.g. jdbc://server/db/schema/table/column/Z   →  jdbc://server/db/schema/table
    /// </summary>
    private static string ExtractParentUrn(string columnUrn)
    {
        var idx = columnUrn.LastIndexOf("/column/", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? columnUrn[..idx] : columnUrn;
    }

    private static string ExtractColumnName(string columnUrn)
    {
        return columnUrn.Split('/').LastOrDefault() ?? columnUrn;
    }

    private static AtlasEntityDto CreateEntityFromUrn(string urn)
    {
        // Determine type and name from URN structure
        string name;
        string platform;
        string type = "DATASET";

        if (urn.StartsWith("nifi://", StringComparison.OrdinalIgnoreCase))
        {
            platform = "NiFi";
            // nifi://container/{id}/processor/{processorId} → extract last segment after /processor/
            var parts = urn.Split('/');
            name = parts.LastOrDefault() ?? urn;
        }
        else if (urn.StartsWith("jdbc://", StringComparison.OrdinalIgnoreCase))
        {
            platform = "MSSQL";
            type = "TABLE";
            // jdbc://server/db/schema/table → last segment is table name
            var parts = urn.Split('/');
            name = parts.LastOrDefault() ?? urn;
        }
        else
        {
            platform = "Unknown";
            name = urn.Split('/').LastOrDefault() ?? urn;
        }

        return new AtlasEntityDto
        {
            Urn = urn,
            Type = type,
            Name = name,
            Platform = platform,
            Description = string.Empty,
            Properties = new Dictionary<string, string>(),
            ParentContainerUrn = null
        };
    }

    private static AtlasEntityDto CreateColumnEntity(string columnUrn)
    {
        var columnName = columnUrn.Split('/').LastOrDefault() ?? "Unknown";
        var platform = columnUrn.StartsWith("nifi://") ? "NiFi" :
                      columnUrn.StartsWith("jdbc://") ? "JDBC" :
                      "Unknown";

        return new AtlasEntityDto
        {
            Urn = columnUrn,
            Type = "COLUMN",
            Name = columnName,
            Platform = platform,
            Description = string.Empty,
            Properties = new Dictionary<string, string>(),
            ParentContainerUrn = null
        };
    }

    private static LineageDirection ParseDirection(string direction)
    {
        return direction.ToUpperInvariant() switch
        {
            "UPSTREAM" => LineageDirection.Upstream,
            "DOWNSTREAM" => LineageDirection.Downstream,
            "BOTH" => LineageDirection.Both,
            _ => LineageDirection.Both
        };
    }

    private static AtlasEntityDto MapToAtlasEntity(NiFiProcessor processor)
    {
        return new AtlasEntityDto
        {
            Urn = processor.Fqn.Value,
            Type = "DATASET",
            Name = processor.Name.Value,
            Platform = "NiFi",
            Description = processor.Description ?? string.Empty,
            Properties = processor.Properties.ToDictionary(),
            ParentContainerUrn = processor.ParentProcessGroupId.Value
        };
    }
}
