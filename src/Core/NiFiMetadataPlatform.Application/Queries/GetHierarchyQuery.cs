using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.DTOs;

namespace NiFiMetadataPlatform.Application.Queries;

/// <summary>
/// Query to get container hierarchy.
/// </summary>
public sealed record GetHierarchyQuery() : IQuery<Result<AtlasHierarchyResponse>>;
