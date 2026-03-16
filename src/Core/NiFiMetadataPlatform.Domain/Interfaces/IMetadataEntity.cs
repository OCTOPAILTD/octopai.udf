namespace NiFiMetadataPlatform.Domain.Interfaces;

/// <summary>
/// Common interface for all metadata entities from various platforms.
/// Provides a unified structure for metadata regardless of source platform.
/// </summary>
public interface IMetadataEntity
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the fully qualified name (FQN) of the entity.
    /// </summary>
    string Fqn { get; }

    /// <summary>
    /// Gets the human-readable name of the entity.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type of the entity (e.g., processor, table, topic, etc.).
    /// </summary>
    string EntityType { get; }

    /// <summary>
    /// Gets the source platform name (e.g., NiFi, Trino, Kafka, etc.).
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Gets the description of the entity.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the platform-specific properties as key-value pairs.
    /// </summary>
    Dictionary<string, string> Properties { get; }

    /// <summary>
    /// Gets the timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the timestamp when the entity was last updated.
    /// </summary>
    DateTime UpdatedAt { get; }
}
