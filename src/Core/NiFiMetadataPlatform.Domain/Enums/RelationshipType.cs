namespace NiFiMetadataPlatform.Domain.Enums;

/// <summary>
/// Represents the type of relationship between entities.
/// </summary>
public enum RelationshipType
{
    /// <summary>
    /// Hierarchical containment (process group contains processor).
    /// </summary>
    Contains,

    /// <summary>
    /// Data lineage (processor produces data for another processor).
    /// </summary>
    Lineage,

    /// <summary>
    /// Generic reference relationship.
    /// </summary>
    References,

    /// <summary>
    /// Connection between processors.
    /// </summary>
    Connection
}
