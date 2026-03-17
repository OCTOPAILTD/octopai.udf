using MediatR;
using Microsoft.AspNetCore.Mvc;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Application.Queries;
using NiFiMetadataPlatform.API.Services;

namespace NiFiMetadataPlatform.API.Controllers;

/// <summary>
/// Metadata API controller for search and entity operations.
/// </summary>
[ApiController]
[Route("api/atlas")]
[Produces("application/json")]
public sealed class MetadataController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly INiFiMetadataIngestionService _ingestionService;
    private readonly ISearchRepository _searchRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataController"/> class.
    /// </summary>
    public MetadataController(IMediator mediator, INiFiMetadataIngestionService ingestionService, ISearchRepository searchRepository)
    {
        _mediator = mediator;
        _ingestionService = ingestionService;
        _searchRepository = searchRepository;
    }

    /// <summary>
    /// Search entities.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query = "*", 
        [FromQuery] int count = 25,
        [FromQuery] string? type_name = null,
        [FromQuery] string? platform = null,
        CancellationToken cancellationToken = default)
    {
        var searchQuery = new SearchEntitiesQuery(query, type_name, platform, count);
        var result = await _mediator.Send(searchQuery, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get platform statistics.
    /// </summary>
    [HttpGet("platforms")]
    public async Task<IActionResult> GetPlatforms(CancellationToken cancellationToken = default)
    {
        var query = new GetPlatformStatsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get entity by qualified name.
    /// </summary>
    [HttpGet("entity/by-qualified-name")]
    public async Task<IActionResult> GetEntityByQualifiedName(
        [FromQuery] string qualified_name,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEntityByUrnQuery(qualified_name);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get columns for a table entity by its URN.
    /// </summary>
    [HttpGet("entity/columns")]
    public async Task<IActionResult> GetTableColumns(
        [FromQuery] string table_urn,
        CancellationToken cancellationToken = default)
    {
        var decodedUrn = Uri.UnescapeDataString(table_urn);
        var result = await _searchRepository.GetColumnsByTableFqnAsync(decodedUrn, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { columns = result.Value, total = result.Value!.Count });
    }

    /// <summary>
    /// Get container hierarchy.
    /// </summary>
    [HttpGet("hierarchy/containers")]
    public async Task<IActionResult> GetHierarchy(CancellationToken cancellationToken = default)
    {
        var query = new GetHierarchyQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get container children.
    /// </summary>
    [HttpGet("container-children/{**urn}")]
    public async Task<IActionResult> GetContainerChildren(string urn, CancellationToken cancellationToken = default)
    {
        var query = new GetEntityChildrenQuery(urn);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get entity lineage.
    /// </summary>
    [HttpGet("lineage/{**urn}")]
    public async Task<IActionResult> GetLineage(
        string urn,
        [FromQuery] string? direction = null,
        [FromQuery] int depth = 3,
        [FromQuery] bool includeColumns = false,
        CancellationToken cancellationToken = default)
    {
        // Decode the URN in case it was double-encoded by the frontend (encodeURIComponent)
        var decodedUrn = Uri.UnescapeDataString(urn);
        urn = decodedUrn;

        // For column URNs, the lineage handler returns entity-level nodes/edges
        // filtered to the single column. We use the parent entity as the center node.
        var isColumnUrn = urn.Contains("/column/", StringComparison.OrdinalIgnoreCase);
        var columnName = isColumnUrn ? urn.Split('/').LastOrDefault() ?? string.Empty : string.Empty;
        var centerNodeUrn = isColumnUrn
            ? urn[..urn.LastIndexOf("/column/", StringComparison.OrdinalIgnoreCase)]
            : urn;

        var query = new GetAtlasLineageQuery(urn, direction ?? "BOTH", depth, includeColumns);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        var lineage = result.Value!;

        // Look up the center entity's name
        var entityQuery = new GetEntityByUrnQuery(centerNodeUrn);
        var entityResult = await _mediator.Send(entityQuery, cancellationToken);
        var centerName = entityResult.IsSuccess && entityResult.Value != null
            ? entityResult.Value.Name
            : centerNodeUrn.Split('/').LastOrDefault() ?? centerNodeUrn;
        var centerPlatform = entityResult.IsSuccess && entityResult.Value != null
            ? entityResult.Value.Platform
            : (centerNodeUrn.StartsWith("nifi://") ? "NiFi" : "Unknown");

        // Collect all entity URNs that will appear as nodes
        var allEntityUrns = new HashSet<string> { centerNodeUrn };
        foreach (var e in lineage.Upstream) allEntityUrns.Add(e.Urn);
        foreach (var e in lineage.Downstream) allEntityUrns.Add(e.Urn);
        foreach (var cl in lineage.ColumnLineage)
        {
            allEntityUrns.Add(cl.FromUrn);
            allEntityUrns.Add(cl.ToUrn);
        }

        // For column lineage: each node shows only the relevant column.
        // For entity lineage: each node shows all its columns.
        object BuildNode(string nodeUrn, string nodeType, string nodeName, string nodePlatform, bool isCenter)
        {
            List<string> cols;
            if (isColumnUrn)
            {
                // Show only the lineage column on each node
                cols = new List<string> { columnName };
            }
            else
            {
                cols = new List<string>();
            }

            return new
            {
                urn = nodeUrn,
                type = nodeType,
                name = nodeName,
                platform = nodePlatform,
                isCenter,
                columns = cols,
            };
        }

        // Build nodes map
        var nodesMap = new Dictionary<string, object>();

        // Add the center entity node
        nodesMap[centerNodeUrn] = BuildNode(centerNodeUrn, "DATASET", centerName, centerPlatform, true);

        foreach (var entity in lineage.Upstream)
        {
            if (!nodesMap.ContainsKey(entity.Urn))
                nodesMap[entity.Urn] = BuildNode(entity.Urn, entity.Type, entity.Name, entity.Platform, false);
        }

        foreach (var entity in lineage.Downstream)
        {
            if (!nodesMap.ContainsKey(entity.Urn))
                nodesMap[entity.Urn] = BuildNode(entity.Urn, entity.Type, entity.Name, entity.Platform, false);
        }

        // Add any nodes referenced in columnLineage that aren't already in nodesMap
        foreach (var cl in lineage.ColumnLineage)
        {
            if (!nodesMap.ContainsKey(cl.FromUrn))
            {
                var fromEntity = lineage.Upstream.FirstOrDefault(e => e.Urn == cl.FromUrn)
                    ?? lineage.Downstream.FirstOrDefault(e => e.Urn == cl.FromUrn);
                if (fromEntity != null)
                    nodesMap[cl.FromUrn] = BuildNode(fromEntity.Urn, fromEntity.Type, fromEntity.Name, fromEntity.Platform, false);
            }

            if (!nodesMap.ContainsKey(cl.ToUrn))
            {
                var toEntity = lineage.Upstream.FirstOrDefault(e => e.Urn == cl.ToUrn)
                    ?? lineage.Downstream.FirstOrDefault(e => e.Urn == cl.ToUrn);
                if (toEntity != null)
                    nodesMap[cl.ToUrn] = BuildNode(toEntity.Urn, toEntity.Type, toEntity.Name, toEntity.Platform, false);
            }
        }

        // For entity lineage (non-column), populate columns from OpenSearch
        if (!isColumnUrn)
        {
            var columnTasks = allEntityUrns.ToDictionary(
                entityUrn => entityUrn,
                entityUrn => _searchRepository.GetColumnUrnsByProcessorFqnAsync(entityUrn, cancellationToken));
            await Task.WhenAll(columnTasks.Values);

            var entityColumns = new Dictionary<string, List<string>>();
            foreach (var (entityUrn, task) in columnTasks)
            {
                var colResult = await task;
                if (colResult.IsSuccess && colResult.Value != null && colResult.Value.Count > 0)
                {
                    entityColumns[entityUrn] = colResult.Value
                        .Select(colUrn => colUrn.Split('/').LastOrDefault() ?? colUrn)
                        .Distinct()
                        .ToList();
                }
            }

            // Rebuild nodes with column data
            var rebuiltNodes = new Dictionary<string, object>();
            foreach (var (nodeUrn, node) in nodesMap)
            {
                entityColumns.TryGetValue(nodeUrn, out var cols);
                var n = (dynamic)node;
                rebuiltNodes[nodeUrn] = new
                {
                    urn = (string)n.urn,
                    type = (string)n.type,
                    name = (string)n.name,
                    platform = (string)n.platform,
                    isCenter = (bool)n.isCenter,
                    columns = cols ?? new List<string>(),
                };
            }

            nodesMap = rebuiltNodes;
        }

        // Build edges from column lineage (source_dataset → target_dataset), deduped, no self-loops
        var edges = new List<object>();
        var edgeSet = new HashSet<string>();

        foreach (var cl in lineage.ColumnLineage)
        {
            if (cl.FromUrn == cl.ToUrn) continue;
            var edgeKey = $"{cl.FromUrn}→{cl.ToUrn}";
            if (edgeSet.Add(edgeKey) && nodesMap.ContainsKey(cl.FromUrn) && nodesMap.ContainsKey(cl.ToUrn))
            {
                edges.Add(new { source = cl.FromUrn, target = cl.ToUrn, type = "lineage" });
            }
        }

        // Map ColumnLineageDto to the frontend's expected field names
        var columnLineage = lineage.ColumnLineage.Select(cl => new
        {
            source_dataset = cl.FromUrn,
            source_field = cl.FromColumn,
            target_dataset = cl.ToUrn,
            target_field = cl.ToColumn
        }).ToList();

        return Ok(new
        {
            nodes = nodesMap.Values.ToList(),
            edges,
            columnLineage
        });
    }

    /// <summary>
    /// Trigger NiFi metadata ingestion from a running container.
    /// </summary>
    [HttpPost("ingest/nifi/{containerId}")]
    public async Task<IActionResult> IngestNiFiMetadata(
        string containerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _ingestionService.IngestFromContainerAsync(containerId, cancellationToken);
            return Ok(new { success = true, entitiesIngested = count, message = $"Successfully ingested {count} entities from NiFi container {containerId}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}
