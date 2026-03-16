using NiFiMetadataPlatform.Application.Common;
using NiFiMetadataPlatform.Domain.Enums;

namespace NiFiMetadataPlatform.Application.Queries.GetLineage;

/// <summary>
/// Query to get processor lineage.
/// </summary>
public sealed record GetLineageQuery : IQuery<Result<LineageDto>>
{
    /// <summary>
    /// Gets the processor FQN.
    /// </summary>
    public required string Fqn { get; init; }

    /// <summary>
    /// Gets the maximum depth to traverse.
    /// </summary>
    public int Depth { get; init; } = 5;

    /// <summary>
    /// Gets the direction of traversal.
    /// </summary>
    public LineageDirection Direction { get; init; } = LineageDirection.Both;
}
