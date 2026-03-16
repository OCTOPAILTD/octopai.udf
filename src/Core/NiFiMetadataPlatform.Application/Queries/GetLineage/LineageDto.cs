using NiFiMetadataPlatform.Application.Common;

namespace NiFiMetadataPlatform.Application.Queries.GetLineage;

/// <summary>
/// Data transfer object for lineage information.
/// </summary>
public sealed record LineageDto
{
    /// <summary>
    /// Gets the root processor FQN.
    /// </summary>
    public required string RootFqn { get; init; }

    /// <summary>
    /// Gets the upstream processors.
    /// </summary>
    public List<ProcessorDto> Upstream { get; init; } = new();

    /// <summary>
    /// Gets the downstream processors.
    /// </summary>
    public List<ProcessorDto> Downstream { get; init; } = new();

    /// <summary>
    /// Gets the total number of processors in the lineage.
    /// </summary>
    public int TotalCount => Upstream.Count + Downstream.Count + 1;
}
