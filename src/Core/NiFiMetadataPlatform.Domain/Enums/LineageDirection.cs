namespace NiFiMetadataPlatform.Domain.Enums;

/// <summary>
/// Represents the direction of lineage traversal.
/// </summary>
public enum LineageDirection
{
    /// <summary>
    /// Traverse upstream (sources).
    /// </summary>
    Upstream,

    /// <summary>
    /// Traverse downstream (targets).
    /// </summary>
    Downstream,

    /// <summary>
    /// Traverse both upstream and downstream.
    /// </summary>
    Both
}
