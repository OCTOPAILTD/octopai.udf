using Microsoft.Extensions.Logging;
using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.DTOs;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Entities;

namespace NiFiMetadataPlatform.Application.Queries.Handlers;

/// <summary>
/// Handler for GetHierarchyQuery.
/// </summary>
public sealed class GetHierarchyQueryHandler : IQueryHandler<GetHierarchyQuery, Result<AtlasHierarchyResponse>>
{
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<GetHierarchyQueryHandler> _logger;

    public GetHierarchyQueryHandler(
        ISearchRepository searchRepository,
        ILogger<GetHierarchyQueryHandler> logger)
    {
        _searchRepository = searchRepository;
        _logger = logger;
    }

    public async Task<Result<AtlasHierarchyResponse>> Handle(
        GetHierarchyQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting container hierarchy");

        var result = await _searchRepository.GetHierarchyAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("Get hierarchy failed: {Error}", result.Error);
            return Result<AtlasHierarchyResponse>.Failure(result.Error!);
        }

        var response = new AtlasHierarchyResponse
        {
            Containers = result.Value!.Select(MapToAtlasEntity).ToList()
        };

        _logger.LogInformation("Hierarchy returned {Count} containers", response.Containers.Count);

        return Result<AtlasHierarchyResponse>.Success(response);
    }

    private static AtlasEntityDto MapToAtlasEntity(NiFiProcessor processor)
    {
        return new AtlasEntityDto
        {
            Urn = processor.Fqn.Value,
            Type = "CONTAINER",
            Name = processor.Name.Value,
            Platform = "NiFi",
            Description = processor.Description ?? string.Empty,
            Properties = processor.Properties.ToDictionary(),
            ParentContainerUrn = processor.ParentProcessGroupId.Value
        };
    }
}
