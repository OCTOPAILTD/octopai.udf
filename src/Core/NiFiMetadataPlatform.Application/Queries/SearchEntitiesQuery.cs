using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.DTOs;

namespace NiFiMetadataPlatform.Application.Queries;

/// <summary>
/// Query to search entities with filters.
/// </summary>
public sealed record SearchEntitiesQuery(
    string Query,
    string? TypeName,
    string? Platform,
    int Count) : IQuery<Result<AtlasSearchResponse>>;
