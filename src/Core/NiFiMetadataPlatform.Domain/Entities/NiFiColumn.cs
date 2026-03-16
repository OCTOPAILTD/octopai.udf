using NiFiMetadataPlatform.Domain.Common;
using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Entities;

/// <summary>
/// Represents a column within a NiFi processor's data flow.
/// </summary>
public sealed class NiFiColumn : Entity<Guid>
{
    /// <summary>
    /// Gets the column FQN.
    /// </summary>
    public ColumnFqn Fqn { get; private set; } = null!;

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the parent processor FQN.
    /// </summary>
    public ProcessorFqn ProcessorFqn { get; private set; } = null!;

    /// <summary>
    /// Gets the data type of the column (if known).
    /// </summary>
    public string? DataType { get; private set; }

    /// <summary>
    /// Gets the column description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets whether the column is nullable.
    /// </summary>
    public bool? IsNullable { get; private set; }

    /// <summary>
    /// Gets the ordinal position of the column in the schema.
    /// </summary>
    public int? OrdinalPosition { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private NiFiColumn()
    {
    }

    /// <summary>
    /// Creates a new NiFi column.
    /// </summary>
    /// <param name="fqn">The column FQN.</param>
    /// <param name="name">The column name.</param>
    /// <param name="processorFqn">The parent processor FQN.</param>
    /// <param name="dataType">The data type (optional).</param>
    /// <param name="description">The description (optional).</param>
    /// <param name="isNullable">Whether the column is nullable (optional).</param>
    /// <param name="ordinalPosition">The ordinal position (optional).</param>
    /// <returns>A new column instance.</returns>
    public static NiFiColumn Create(
        ColumnFqn fqn,
        string name,
        ProcessorFqn processorFqn,
        string? dataType = null,
        string? description = null,
        bool? isNullable = null,
        int? ordinalPosition = null)
    {
        ArgumentNullException.ThrowIfNull(fqn);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        ArgumentNullException.ThrowIfNull(processorFqn);

        var column = new NiFiColumn
        {
            Id = Guid.NewGuid(),
            Fqn = fqn,
            Name = name,
            ProcessorFqn = processorFqn,
            DataType = dataType,
            Description = description,
            IsNullable = isNullable,
            OrdinalPosition = ordinalPosition,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return column;
    }

    /// <summary>
    /// Updates the column metadata.
    /// </summary>
    /// <param name="dataType">The data type.</param>
    /// <param name="description">The description.</param>
    /// <param name="isNullable">Whether the column is nullable.</param>
    public void UpdateMetadata(string? dataType, string? description, bool? isNullable)
    {
        DataType = dataType;
        Description = description;
        IsNullable = isNullable;
        UpdatedAt = DateTime.UtcNow;
    }
}
