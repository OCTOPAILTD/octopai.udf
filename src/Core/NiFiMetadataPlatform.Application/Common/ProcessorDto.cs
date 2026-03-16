namespace NiFiMetadataPlatform.Application.Common;

/// <summary>
/// Data transfer object for processor information.
/// </summary>
public sealed record ProcessorDto
{
    /// <summary>
    /// Gets the processor ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the processor FQN.
    /// </summary>
    public required string Fqn { get; init; }

    /// <summary>
    /// Gets the processor name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the processor type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the processor status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the processor properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; init; } = new();

    /// <summary>
    /// Gets the parent process group ID.
    /// </summary>
    public required string ParentProcessGroupId { get; init; }

    /// <summary>
    /// Gets the processor description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the processor owner.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Gets the processor tags.
    /// </summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}
