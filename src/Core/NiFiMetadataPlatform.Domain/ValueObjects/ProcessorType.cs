namespace NiFiMetadataPlatform.Domain.ValueObjects;

/// <summary>
/// Represents a NiFi processor type (fully qualified class name).
/// </summary>
public sealed record ProcessorType
{
    private const string ValidPrefix = "org.apache.nifi.processors.";

    /// <summary>
    /// Gets the processor type value.
    /// </summary>
    public string Value { get; }

    private ProcessorType(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Parses a processor type from a string.
    /// </summary>
    /// <param name="value">The processor type string.</param>
    /// <returns>A processor type.</returns>
    /// <exception cref="ArgumentException">Thrown when the type is invalid.</exception>
    public static ProcessorType Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Processor type cannot be empty", nameof(value));
        }

        if (!value.StartsWith(ValidPrefix, StringComparison.OrdinalIgnoreCase) &&
            !value.Contains('.'))
        {
            throw new ArgumentException(
                $"Processor type must be a fully qualified class name (e.g., {ValidPrefix}standard.ExecuteSQL)",
                nameof(value));
        }

        return new ProcessorType(value);
    }

    /// <summary>
    /// Checks if this is an ExecuteSQL processor.
    /// </summary>
    /// <returns>True if ExecuteSQL processor, false otherwise.</returns>
    public bool IsExecuteSql() =>
        Value.Contains("ExecuteSQL", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if this is a database processor.
    /// </summary>
    /// <returns>True if database processor, false otherwise.</returns>
    public bool IsDatabaseProcessor() =>
        Value.Contains("Database", StringComparison.OrdinalIgnoreCase) ||
        IsExecuteSql();

    /// <summary>
    /// Gets the simple name of the processor (last part of the class name).
    /// </summary>
    /// <returns>The simple name.</returns>
    public string GetSimpleName()
    {
        var lastDot = Value.LastIndexOf('.');
        return lastDot >= 0 ? Value[(lastDot + 1)..] : Value;
    }

    /// <summary>
    /// Returns the string representation of the processor type.
    /// </summary>
    /// <returns>The type value.</returns>
    public override string ToString() => Value;
}
