using Microsoft.Extensions.Logging;
using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.DTOs;
using NiFiMetadataPlatform.Application.Interfaces;

namespace NiFiMetadataPlatform.Application.Queries.Handlers;

/// <summary>
/// Handler for GetPlatformStatsQuery.
/// </summary>
public sealed class GetPlatformStatsQueryHandler : IQueryHandler<GetPlatformStatsQuery, Result<AtlasPlatformStatsResponse>>
{
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<GetPlatformStatsQueryHandler> _logger;

    public GetPlatformStatsQueryHandler(
        ISearchRepository searchRepository,
        ILogger<GetPlatformStatsQueryHandler> logger)
    {
        _searchRepository = searchRepository;
        _logger = logger;
    }

    public async Task<Result<AtlasPlatformStatsResponse>> Handle(
        GetPlatformStatsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting platform statistics");

        var result = await _searchRepository.GetPlatformStatsAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogError("Get platform stats failed: {Error}", result.Error);
            return Result<AtlasPlatformStatsResponse>.Failure(result.Error!);
        }

        var response = new AtlasPlatformStatsResponse
        {
            Platforms = result.Value!
                .Select(kvp => new PlatformStatDto
                {
                    Platform = kvp.Key,
                    Count = kvp.Value
                })
                .OrderByDescending(p => p.Count)
                .ToList()
        };

        _logger.LogInformation("Platform stats returned {Count} platforms", response.Platforms.Count);

        return Result<AtlasPlatformStatsResponse>.Success(response);
    }
}
