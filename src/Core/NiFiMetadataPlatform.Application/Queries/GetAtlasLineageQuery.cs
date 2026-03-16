using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.DTOs;

namespace NiFiMetadataPlatform.Application.Queries;

/// <summary>
/// Query to get lineage in Atlas-compatible format.
/// </summary>
public sealed record GetAtlasLineageQuery(
    string Urn,
    string Direction,
    int MaxHops,
    bool IncludeColumns) : IQuery<Result<AtlasLineageResponse>>;
