using Microsoft.Extensions.Logging;
using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Domain.Entities;

namespace NiFiMetadataPlatform.Application.Queries.GetProcessor;

/// <summary>
/// Handler for GetProcessorQuery.
/// </summary>
public sealed class GetProcessorQueryHandler
    : IQueryHandler<GetProcessorQuery, Result<ProcessorDto?>>
{
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<GetProcessorQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetProcessorQueryHandler"/> class.
    /// </summary>
    /// <param name="searchRepository">The search repository.</param>
    /// <param name="logger">The logger.</param>
    public GetProcessorQueryHandler(
        ISearchRepository searchRepository,
        ILogger<GetProcessorQueryHandler> logger)
    {
        _searchRepository = searchRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the GetProcessorQuery.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the processor DTO, or null if not found.</returns>
    public async Task<Result<ProcessorDto?>> Handle(
        GetProcessorQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting processor {Fqn}", query.Fqn);

            var result = await _searchRepository.GetByFqnAsync(query.Fqn, cancellationToken);

            if (result.IsFailure)
            {
                return Result<ProcessorDto?>.Failure(result.Error!);
            }

            if (result.Value == null)
            {
                _logger.LogInformation("Processor {Fqn} not found", query.Fqn);
                return Result<ProcessorDto?>.Success(null);
            }

            var dto = MapToDto(result.Value);
            return Result<ProcessorDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting processor {Fqn}", query.Fqn);
            return Result<ProcessorDto?>.Failure($"Failed to get processor: {ex.Message}");
        }
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
