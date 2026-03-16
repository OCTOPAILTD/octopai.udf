namespace NiFiMetadataPlatform.Domain.ValueObjects;

/// <summary>
/// Represents a Fully Qualified Name (FQN) for database entities.
/// Formats:
/// - Database: jdbc://{server}/{database}
/// - Schema: jdbc://{server}/{database}/{schema}
/// - Table: jdbc://{server}/{database}/{schema}/{table}
/// - Column: jdbc://{server}/{database}/{schema}/{table}/column/{columnName}
/// </summary>
public sealed record DatabaseFqn
{
    private const string Prefix = "jdbc://";
    private const string ColumnSegment = "column";

    /// <summary>
    /// Gets the FQN value.
    /// </summary>
    public string Value { get; }

    private DatabaseFqn(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a database FQN.
    /// </summary>
    /// <param name="server">The server name.</param>
    /// <param name="database">The database name.</param>
    /// <returns>A database FQN.</returns>
    public static DatabaseFqn CreateDatabase(string server, string database)
    {
        ValidateSegment(server, nameof(server));
        ValidateSegment(database, nameof(database));

        return new DatabaseFqn($"{Prefix}{server}/{database}");
    }

    /// <summary>
    /// Creates a schema FQN.
    /// </summary>
    /// <param name="server">The server name.</param>
    /// <param name="database">The database name.</param>
    /// <param name="schema">The schema name.</param>
    /// <returns>A schema FQN.</returns>
    public static DatabaseFqn CreateSchema(string server, string database, string schema)
    {
        ValidateSegment(server, nameof(server));
        ValidateSegment(database, nameof(database));
        ValidateSegment(schema, nameof(schema));

        return new DatabaseFqn($"{Prefix}{server}/{database}/{schema}");
    }

    /// <summary>
    /// Creates a table FQN.
    /// </summary>
    /// <param name="server">The server name.</param>
    /// <param name="database">The database name.</param>
    /// <param name="schema">The schema name.</param>
    /// <param name="table">The table name.</param>
    /// <returns>A table FQN.</returns>
    public static DatabaseFqn CreateTable(string server, string database, string schema, string table)
    {
        ValidateSegment(server, nameof(server));
        ValidateSegment(database, nameof(database));
        ValidateSegment(schema, nameof(schema));
        ValidateSegment(table, nameof(table));

        return new DatabaseFqn($"{Prefix}{server}/{database}/{schema}/{table}");
    }

    /// <summary>
    /// Creates a column FQN.
    /// </summary>
    /// <param name="server">The server name.</param>
    /// <param name="database">The database name.</param>
    /// <param name="schema">The schema name.</param>
    /// <param name="table">The table name.</param>
    /// <param name="columnName">The column name.</param>
    /// <returns>A column FQN.</returns>
    public static DatabaseFqn CreateColumn(string server, string database, string schema, string table, string columnName)
    {
        ValidateSegment(server, nameof(server));
        ValidateSegment(database, nameof(database));
        ValidateSegment(schema, nameof(schema));
        ValidateSegment(table, nameof(table));
        ValidateSegment(columnName, nameof(columnName));

        return new DatabaseFqn($"{Prefix}{server}/{database}/{schema}/{table}/{ColumnSegment}/{columnName}");
    }

    /// <summary>
    /// Creates a column FQN from a table FQN.
    /// </summary>
    /// <param name="tableFqn">The table FQN.</param>
    /// <param name="columnName">The column name.</param>
    /// <returns>A column FQN.</returns>
    public static DatabaseFqn CreateColumnFromTable(DatabaseFqn tableFqn, string columnName)
    {
        ValidateSegment(columnName, nameof(columnName));

        if (tableFqn.Value.Contains($"/{ColumnSegment}/"))
        {
            throw new ArgumentException("Cannot create column from a column FQN", nameof(tableFqn));
        }

        return new DatabaseFqn($"{tableFqn.Value}/{ColumnSegment}/{columnName}");
    }

    /// <summary>
    /// Parses a database FQN from a string.
    /// </summary>
    /// <param name="fqn">The FQN string.</param>
    /// <returns>A database FQN.</returns>
    /// <exception cref="ArgumentException">Thrown when the FQN format is invalid.</exception>
    public static DatabaseFqn Parse(string fqn)
    {
        if (string.IsNullOrWhiteSpace(fqn))
        {
            throw new ArgumentException("FQN cannot be empty", nameof(fqn));
        }

        if (!fqn.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"FQN must start with {Prefix}", nameof(fqn));
        }

        return new DatabaseFqn(fqn);
    }

    /// <summary>
    /// Tries to parse a database FQN from a string.
    /// </summary>
    /// <param name="fqn">The FQN string.</param>
    /// <param name="databaseFqn">The parsed database FQN.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string fqn, out DatabaseFqn? databaseFqn)
    {
        databaseFqn = null;

        try
        {
            databaseFqn = Parse(fqn);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if this FQN represents a column.
    /// </summary>
    /// <returns>True if this is a column FQN, false otherwise.</returns>
    public bool IsColumn() => Value.Contains($"/{ColumnSegment}/");

    /// <summary>
    /// Gets the parent FQN (e.g., table FQN for a column, schema FQN for a table).
    /// </summary>
    /// <returns>The parent FQN, or null if this is a database FQN.</returns>
    public DatabaseFqn? GetParentFqn()
    {
        var parts = Value[Prefix.Length..].Split('/');
        
        if (parts.Length <= 2)
        {
            return null; // Database level, no parent
        }

        // If it's a column, parent is the table (remove /column/{name})
        if (IsColumn())
        {
            var lastColumnIndex = Value.LastIndexOf($"/{ColumnSegment}/");
            return new DatabaseFqn(Value[..lastColumnIndex]);
        }

        // Otherwise, parent is one level up
        var lastSlashIndex = Value.LastIndexOf('/');
        return new DatabaseFqn(Value[..lastSlashIndex]);
    }

    private static void ValidateSegment(string segment, string paramName)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            throw new ArgumentException($"{paramName} cannot be empty", paramName);
        }
    }

    /// <summary>
    /// Returns the string representation of the FQN.
    /// </summary>
    /// <returns>The FQN value.</returns>
    public override string ToString() => Value;
}
