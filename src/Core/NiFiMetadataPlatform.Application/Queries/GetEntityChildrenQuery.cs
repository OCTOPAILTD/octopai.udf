using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Application.DTOs;

namespace NiFiMetadataPlatform.Application.Queries;

/// <summary>
/// Query to get children of a container entity.
/// </summary>
public sealed record GetEntityChildrenQuery(string Urn) : IQuery<Result<AtlasChildrenResponse>>;
