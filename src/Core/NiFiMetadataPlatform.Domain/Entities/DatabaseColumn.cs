using NiFiMetadataPlatform.Domain.Common;
using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Entities;

/// <summary>
/// Represents a database column entity.
/// </summary>
public sealed class DatabaseColumn : Entity<Guid>
{
    /// <summary>
    /// Gets the column FQN.
    /// </summary>
    public DatabaseFqn Fqn { get; private set; } = null!;

    /// <summary>
    /// Gets the column name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the parent table FQN.
    /// </summary>
    public DatabaseFqn TableFqn { get; private set; } = null!;

    /// <summary>
    /// Gets the data type of the column.
    /// </summary>
    public string DataType { get; private set; } = null!;

    /// <summary>
    /// Gets the column description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets whether the column is nullable.
    /// </summary>
    public bool IsNullable { get; private set; }

    /// <summary>
    /// Gets whether the column is a primary key.
    /// </summary>
    public bool IsPrimaryKey { get; private set; }

    /// <summary>
    /// Gets the ordinal position of the column in the table.
    /// </summary>
    public int OrdinalPosition { get; private set; }

    /// <summary>
    /// Gets the default value of the column.
    /// </summary>
    public string? DefaultValue { get; private set; }

    /// <summary>
    /// Gets the maximum length of the column (for string types).
    /// </summary>
    public int? MaxLength { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private DatabaseColumn()
    {
    }

    /// <summary>
    /// Creates a new database column.
    /// </summary>
    /// <param name="fqn">The column FQN.</param>
    /// <param name="name">The column name.</param>
    /// <param name="tableFqn">The parent table FQN.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="isNullable">Whether the column is nullable.</param>
    /// <param name="ordinalPosition">The ordinal position.</param>
    /// <param name="description">The description (optional).</param>
    /// <param name="isPrimaryKey">Whether the column is a primary key (optional).</param>
    /// <param name="defaultValue">The default value (optional).</param>
    /// <param name="maxLength">The maximum length (optional).</param>
    /// <returns>A new column instance.</returns>
    public static DatabaseColumn Create(
        DatabaseFqn fqn,
        string name,
        DatabaseFqn tableFqn,
        string dataType,
        bool isNullable,
        int ordinalPosition,
        string? description = null,
        bool isPrimaryKey = false,
        string? defaultValue = null,
        int? maxLength = null)
    {
        ArgumentNullException.ThrowIfNull(fqn);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        ArgumentNullException.ThrowIfNull(tableFqn);
        if (string.IsNullOrWhiteSpace(dataType))
            throw new ArgumentException("DataType cannot be empty", nameof(dataType));

        var column = new DatabaseColumn
        {
            Id = Guid.NewGuid(),
            Fqn = fqn,
            Name = name,
            TableFqn = tableFqn,
            DataType = dataType,
            IsNullable = isNullable,
            OrdinalPosition = ordinalPosition,
            Description = description,
            IsPrimaryKey = isPrimaryKey,
            DefaultValue = defaultValue,
            MaxLength = maxLength,
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
    /// <param name="isPrimaryKey">Whether the column is a primary key.</param>
    public void UpdateMetadata(string dataType, string? description, bool isNullable, bool isPrimaryKey)
    {
        DataType = dataType;
        Description = description;
        IsNullable = isNullable;
        IsPrimaryKey = isPrimaryKey;
        UpdatedAt = DateTime.UtcNow;
    }
}
