using Microsoft.Extensions.Logging;
using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.DTOs;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Entities;

namespace NiFiMetadataPlatform.Application.Queries.Handlers;

/// <summary>
/// Handler for GetEntityChildrenQuery.
/// </summary>
public sealed class GetEntityChildrenQueryHandler : IQueryHandler<GetEntityChildrenQuery, Result<AtlasChildrenResponse>>
{
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<GetEntityChildrenQueryHandler> _logger;

    public GetEntityChildrenQueryHandler(
        ISearchRepository searchRepository,
        ILogger<GetEntityChildrenQueryHandler> logger)
    {
        _searchRepository = searchRepository;
        _logger = logger;
    }

    public async Task<Result<AtlasChildrenResponse>> Handle(
        GetEntityChildrenQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting children for entity: {Urn}", request.Urn);

        var result = await _searchRepository.GetChildrenAsync(request.Urn, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("Get children failed for {Urn}: {Error}", request.Urn, result.Error);
            return Result<AtlasChildrenResponse>.Failure(result.Error!);
        }

        var response = new AtlasChildrenResponse
        {
            Children = result.Value!.Select(MapToAtlasEntity).ToList(),
            Total = result.Value!.Count
        };

        _logger.LogInformation("Children for {Urn} returned {Count} results", request.Urn, response.Total);

        return Result<AtlasChildrenResponse>.Success(response);
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
