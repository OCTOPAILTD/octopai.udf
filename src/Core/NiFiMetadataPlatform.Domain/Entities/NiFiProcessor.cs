using NiFiMetadataPlatform.Domain.Common;
using NiFiMetadataPlatform.Domain.Enums;
using NiFiMetadataPlatform.Domain.Events;
using NiFiMetadataPlatform.Domain.Exceptions;
using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Entities;

/// <summary>
/// Represents a NiFi processor entity with rich domain behavior.
/// </summary>
public sealed class NiFiProcessor : Entity<ProcessorId>, IAggregateRoot
{
    private readonly List<string> _tags = new();

    /// <summary>
    /// Gets the processor FQN.
    /// </summary>
    public ProcessorFqn Fqn { get; private set; } = null!;

    /// <summary>
    /// Gets the processor name.
    /// </summary>
    public ProcessorName Name { get; private set; } = null!;

    /// <summary>
    /// Gets the processor type.
    /// </summary>
    public ProcessorType Type { get; private set; } = null!;

    /// <summary>
    /// Gets the processor status.
    /// </summary>
    public ProcessorStatus Status { get; private set; }

    /// <summary>
    /// Gets the processor properties.
    /// </summary>
    public ProcessorProperties Properties { get; private set; } = ProcessorProperties.Empty();

    /// <summary>
    /// Gets the parent process group ID.
    /// </summary>
    public ProcessGroupId ParentProcessGroupId { get; private set; } = null!;

    /// <summary>
    /// Gets the processor description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the processor owner.
    /// </summary>
    public string? Owner { get; private set; }

    /// <summary>
    /// Gets the processor tags.
    /// </summary>
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the user who created the processor.
    /// </summary>
    public string? CreatedBy { get; private set; }

    private NiFiProcessor()
    {
    }

    /// <summary>
    /// Creates a new NiFi processor.
    /// </summary>
    /// <param name="fqn">The processor FQN.</param>
    /// <param name="name">The processor name.</param>
    /// <param name="type">The processor type.</param>
    /// <param name="parentProcessGroupId">The parent process group ID.</param>
    /// <returns>A new processor instance.</returns>
    public static NiFiProcessor Create(
        ProcessorFqn fqn,
        ProcessorName name,
        ProcessorType type,
        ProcessGroupId parentProcessGroupId)
    {
        ArgumentNullException.ThrowIfNull(fqn);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(parentProcessGroupId);

        var processor = new NiFiProcessor
        {
            Id = ProcessorId.CreateNew(),
            Fqn = fqn,
            Name = name,
            Type = type,
            Status = ProcessorStatus.Active,
            ParentProcessGroupId = parentProcessGroupId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        processor.AddDomainEvent(new ProcessorCreatedEvent(processor.Id, processor.Fqn));

        return processor;
    }

    /// <summary>
    /// Updates the processor properties.
    /// </summary>
    /// <param name="properties">The new properties.</param>
    public void UpdateProperties(ProcessorProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        Properties = properties;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProcessorPropertiesUpdatedEvent(Id, Fqn, properties));
    }

    /// <summary>
    /// Updates the processor description.
    /// </summary>
    /// <param name="description">The new description.</param>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the processor owner.
    /// </summary>
    /// <param name="owner">The owner name.</param>
    public void SetOwner(string? owner)
    {
        Owner = owner?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a tag to the processor.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag cannot be empty", nameof(tag));
        }

        var trimmedTag = tag.Trim();
        if (!_tags.Contains(trimmedTag, StringComparer.OrdinalIgnoreCase))
        {
            _tags.Add(trimmedTag);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Removes a tag from the processor.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var removed = _tags.RemoveAll(t =>
            t.Equals(tag.Trim(), StringComparison.OrdinalIgnoreCase)) > 0;

        if (removed)
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Deactivates the processor.
    /// </summary>
    /// <exception cref="InvalidProcessorStateException">Thrown when the processor is already deleted.</exception>
    public void Deactivate()
    {
        if (Status == ProcessorStatus.Deleted)
        {
            throw new InvalidProcessorStateException("Cannot deactivate a deleted processor");
        }

        Status = ProcessorStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProcessorDeactivatedEvent(Id, Fqn));
    }

    /// <summary>
    /// Activates the processor.
    /// </summary>
    /// <exception cref="InvalidProcessorStateException">Thrown when the processor is deleted.</exception>
    public void Activate()
    {
        if (Status == ProcessorStatus.Deleted)
        {
            throw new InvalidProcessorStateException("Cannot activate a deleted processor");
        }

        Status = ProcessorStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the processor as deleted (soft delete).
    /// </summary>
    public void Delete()
    {
        Status = ProcessorStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProcessorDeletedEvent(Id, Fqn));
    }

    /// <summary>
    /// Checks if the processor has SQL queries.
    /// </summary>
    /// <returns>True if the processor has SQL queries, false otherwise.</returns>
    public bool HasSqlQuery() =>
        Type.IsExecuteSql() && Properties.GetSqlQuery() != null;

    /// <summary>
    /// Gets the SQL query if available.
    /// </summary>
    /// <returns>The SQL query, or null if not available.</returns>
    public string? GetSqlQuery() => Properties.GetSqlQuery();
}
