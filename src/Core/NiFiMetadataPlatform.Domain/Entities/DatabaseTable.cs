using NiFiMetadataPlatform.Domain.Common;
using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Entities;

/// <summary>
/// Represents a database table entity.
/// </summary>
public sealed class DatabaseTable : Entity<Guid>
{
    /// <summary>
    /// Gets the table FQN.
    /// </summary>
    public DatabaseFqn Fqn { get; private set; } = null!;

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the database server.
    /// </summary>
    public string Server { get; private set; } = null!;

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string Database { get; private set; } = null!;

    /// <summary>
    /// Gets the schema name.
    /// </summary>
    public string Schema { get; private set; } = null!;

    /// <summary>
    /// Gets the database platform (e.g., "MSSQL", "PostgreSQL", "MySQL").
    /// </summary>
    public string Platform { get; private set; } = null!;

    /// <summary>
    /// Gets the table description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the table type (e.g., "TABLE", "VIEW").
    /// </summary>
    public string? TableType { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private DatabaseTable()
    {
    }

    /// <summary>
    /// Creates a new database table.
    /// </summary>
    /// <param name="fqn">The table FQN.</param>
    /// <param name="name">The table name.</param>
    /// <param name="server">The server name.</param>
    /// <param name="database">The database name.</param>
    /// <param name="schema">The schema name.</param>
    /// <param name="platform">The database platform.</param>
    /// <param name="description">The description (optional).</param>
    /// <param name="tableType">The table type (optional).</param>
    /// <returns>A new table instance.</returns>
    public static DatabaseTable Create(
        DatabaseFqn fqn,
        string name,
        string server,
        string database,
        string schema,
        string platform,
        string? description = null,
        string? tableType = "TABLE")
    {
        ArgumentNullException.ThrowIfNull(fqn);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(server))
            throw new ArgumentException("Server cannot be empty", nameof(server));
        if (string.IsNullOrWhiteSpace(database))
            throw new ArgumentException("Database cannot be empty", nameof(database));
        if (string.IsNullOrWhiteSpace(schema))
            throw new ArgumentException("Schema cannot be empty", nameof(schema));
        if (string.IsNullOrWhiteSpace(platform))
            throw new ArgumentException("Platform cannot be empty", nameof(platform));

        var table = new DatabaseTable
        {
            Id = Guid.NewGuid(),
            Fqn = fqn,
            Name = name,
            Server = server,
            Database = database,
            Schema = schema,
            Platform = platform,
            Description = description,
            TableType = tableType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return table;
    }

    /// <summary>
    /// Updates the table metadata.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <param name="tableType">The table type.</param>
    public void UpdateMetadata(string? description, string? tableType)
    {
        Description = description;
        TableType = tableType;
        UpdatedAt = DateTime.UtcNow;
    }
}
