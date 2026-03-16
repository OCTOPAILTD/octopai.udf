using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.DTOs;

namespace NiFiMetadataPlatform.Application.Queries;

/// <summary>
/// Query to get platform statistics.
/// </summary>
public sealed record GetPlatformStatsQuery() : IQuery<Result<AtlasPlatformStatsResponse>>;
