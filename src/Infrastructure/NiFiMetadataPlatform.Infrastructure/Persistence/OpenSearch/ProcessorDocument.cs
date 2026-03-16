namespace NiFiMetadataPlatform.Infrastructure.Persistence.OpenSearch;

/// <summary>
/// OpenSearch document model for NiFi processors.
/// Contains all processor properties for search and retrieval.
/// </summary>
public sealed class ProcessorDocument
{
    /// <summary>
    /// Gets or sets the processor FQN (used as document ID).
    /// </summary>
    public string Fqn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processor name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processor type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent process group ID.
    /// </summary>
    public string ParentProcessGroupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processor status.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Gets or sets the processor description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the processor owner.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets the processor properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the processor tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the platform (always "NiFi").
    /// </summary>
    public string Platform { get; set; } = "NiFi";

    /// <summary>
    /// Gets or sets the created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
