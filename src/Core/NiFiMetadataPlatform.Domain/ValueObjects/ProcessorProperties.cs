namespace NiFiMetadataPlatform.Domain.ValueObjects;

/// <summary>
/// Represents processor configuration properties.
/// </summary>
public sealed record ProcessorProperties
{
    /// <summary>
    /// Gets the properties dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, string> Values { get; }

    private ProcessorProperties(IReadOnlyDictionary<string, string> values)
    {
        Values = values;
    }

    /// <summary>
    /// Creates processor properties from a dictionary.
    /// </summary>
    /// <param name="properties">The properties dictionary.</param>
    /// <returns>Processor properties.</returns>
    public static ProcessorProperties Create(Dictionary<string, string> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        var cleanedProperties = properties
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
            .ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value ?? string.Empty);

        return new ProcessorProperties(cleanedProperties);
    }

    /// <summary>
    /// Creates empty processor properties.
    /// </summary>
    /// <returns>Empty processor properties.</returns>
    public static ProcessorProperties Empty() =>
        new(new Dictionary<string, string>());

    /// <summary>
    /// Gets a property value by key.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or null if not found.</returns>
    public string? GetValue(string key) =>
        Values.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    /// Checks if a property exists.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <returns>True if the property exists, false otherwise.</returns>
    public bool HasProperty(string key) => Values.ContainsKey(key);

    /// <summary>
    /// Gets the SQL query if this is an ExecuteSQL processor.
    /// </summary>
    /// <returns>The SQL query, or null if not found.</returns>
    public string? GetSqlQuery() =>
        GetValue("SQL select query") ??
        GetValue("SQL Query") ??
        GetValue("sql");

    /// <summary>
    /// Converts to a dictionary.
    /// </summary>
    /// <returns>A dictionary of properties.</returns>
    public Dictionary<string, string> ToDictionary() =>
        new(Values);
}
