using MediatR;
using NiFiMetadataPlatform.Application.DTOs;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Common;

namespace NiFiMetadataPlatform.Application.Queries.Handlers;

public sealed class GetEntityByUrnQueryHandler : IRequestHandler<GetEntityByUrnQuery, Result<AtlasEntityDto>>
{
    private readonly ISearchRepository _searchRepository;

    public GetEntityByUrnQueryHandler(ISearchRepository searchRepository)
    {
        _searchRepository = searchRepository;
    }

    public async Task<Result<AtlasEntityDto>> Handle(GetEntityByUrnQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Urn))
        {
            return Result<AtlasEntityDto>.Failure("URN cannot be empty");
        }

        // Use the generic raw entity lookup - works for processors, tables, columns, etc.
        var rawResult = await _searchRepository.GetRawEntityByFqnAsync(request.Urn, cancellationToken);

        if (rawResult.IsSuccess && rawResult.Value != null)
        {
            return Result<AtlasEntityDto>.Success(rawResult.Value);
        }

        // Fallback: try the NiFi processor-specific lookup
        var processorResult = await _searchRepository.GetByFqnAsync(request.Urn, cancellationToken);

        if (!processorResult.IsSuccess || processorResult.Value == null)
        {
            return Result<AtlasEntityDto>.Failure($"Entity not found: {request.Urn}");
        }

        var processor = processorResult.Value;

        return Result<AtlasEntityDto>.Success(new AtlasEntityDto
        {
            Urn = processor.Fqn.Value,
            Type = "DATASET",
            Name = processor.Name.Value,
            Platform = "NiFi",
            Description = processor.Description,
            Properties = processor.Properties?.ToDictionary() ?? new Dictionary<string, string>(),
            ParentContainerUrn = processor.ParentProcessGroupId?.Value
        });
    }
}
