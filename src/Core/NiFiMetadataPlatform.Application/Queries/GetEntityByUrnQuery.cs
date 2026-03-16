using MediatR;
using NiFiMetadataPlatform.Application.DTOs;
using NiFiMetadataPlatform.Domain.Common;

namespace NiFiMetadataPlatform.Application.Queries;

public sealed record GetEntityByUrnQuery(string Urn) : IRequest<Result<AtlasEntityDto>>;
