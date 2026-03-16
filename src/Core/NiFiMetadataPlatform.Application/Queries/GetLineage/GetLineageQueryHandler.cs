using Microsoft.Extensions.Logging;
using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Entities;
using NiFiMetadataPlatform.Domain.Enums;

namespace NiFiMetadataPlatform.Application.Queries.GetLineage;

/// <summary>
/// Handler for GetLineageQuery.
/// </summary>
public sealed class GetLineageQueryHandler
    : IQueryHandler<GetLineageQuery, Result<LineageDto>>
{
    private readonly IGraphRepository _graphRepository;
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<GetLineageQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetLineageQueryHandler"/> class.
    /// </summary>
    /// <param name="graphRepository">The graph repository.</param>
    /// <param name="searchRepository">The search repository.</param>
    /// <param name="logger">The logger.</param>
    public GetLineageQueryHandler(
        IGraphRepository graphRepository,
        ISearchRepository searchRepository,
        ILogger<GetLineageQueryHandler> logger)
    {
        _graphRepository = graphRepository;
        _searchRepository = searchRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the GetLineageQuery.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the lineage DTO.</returns>
    public async Task<Result<LineageDto>> Handle(
        GetLineageQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            // Determine if this is a column URN or processor URN
            var isColumnUrn = query.Fqn.Contains("/column/", StringComparison.OrdinalIgnoreCase);

            if (isColumnUrn)
            {
                return await HandleColumnLineageAsync(query, cancellationToken);
            }
            else
            {
                return await HandleProcessorLineageAsync(query, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lineage for {Fqn}", query.Fqn);
            return Result<LineageDto>.Failure($"Failed to get lineage: {ex.Message}");
        }
    }

    private async Task<Result<LineageDto>> HandleColumnLineageAsync(
        GetLineageQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting column lineage for {Fqn} with depth {Depth} and direction {Direction}",
            query.Fqn,
            query.Depth,
            query.Direction);

        var upstreamUrns = new List<string>();
        var downstreamUrns = new List<string>();

        if (query.Direction is LineageDirection.Upstream or LineageDirection.Both)
        {
            var upstreamResult = await _graphRepository.TraverseColumnLineageAsync(
                query.Fqn,
                query.Depth,
                LineageDirection.Upstream,
                cancellationToken);

            if (upstreamResult.IsFailure)
            {
                return Result<LineageDto>.Failure(upstreamResult.Error!);
            }

            upstreamUrns = upstreamResult.Value!;
        }

        if (query.Direction is LineageDirection.Downstream or LineageDirection.Both)
        {
            var downstreamResult = await _graphRepository.TraverseColumnLineageAsync(
                query.Fqn,
                query.Depth,
                LineageDirection.Downstream,
                cancellationToken);

            if (downstreamResult.IsFailure)
            {
                return Result<LineageDto>.Failure(downstreamResult.Error!);
            }

            downstreamUrns = downstreamResult.Value!;
        }

        // For column lineage, we return the URNs but need to fetch metadata from OpenSearch
        // Since LineageDto expects ProcessorDto, we'll need to adapt this
        // For now, return empty lineage (column lineage UI will need separate DTO)
        _logger.LogInformation(
            "Retrieved column lineage for {Fqn}: {UpstreamCount} upstream, {DownstreamCount} downstream",
            query.Fqn,
            upstreamUrns.Count,
            downstreamUrns.Count);

        return Result<LineageDto>.Success(new LineageDto
        {
            RootFqn = query.Fqn
        });
    }

    private async Task<Result<LineageDto>> HandleProcessorLineageAsync(
        GetLineageQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting processor lineage for {Fqn} with depth {Depth} and direction {Direction}",
            query.Fqn,
            query.Depth,
            query.Direction);

        // For processor lineage, we need to build it from OpenSearch connections
        // Since processors are no longer in ArangoDB, we derive lineage from OpenSearch metadata
        // For now, return empty lineage (processor lineage will be derived from OpenSearch queries)
        
        _logger.LogInformation("Processor lineage is now derived from OpenSearch connections");

        return Result<LineageDto>.Success(new LineageDto
        {
            RootFqn = query.Fqn
        });
    }

    private static ProcessorDto MapToDto(NiFiProcessor processor)
    {
        return new ProcessorDto
        {
            Id = processor.Id.Value.ToString(),
            Fqn = processor.Fqn.Value,
            Name = processor.Name.Value,
            Type = processor.Type.Value,
            Status = processor.Status.ToString(),
            Properties = processor.Properties.ToDictionary(),
            ParentProcessGroupId = processor.ParentProcessGroupId.Value,
            Description = processor.Description,
            Owner = processor.Owner,
            Tags = processor.Tags.ToList(),
            CreatedAt = processor.CreatedAt,
            UpdatedAt = processor.UpdatedAt
        };
    }
}
