namespace NiFiMetadataPlatform.Domain.ValueObjects;

/// <summary>
/// Represents a processor name with validation.
/// </summary>
public sealed record ProcessorName
{
    private const int MaxLength = 500;

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Value { get; }

    private ProcessorName(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a processor name.
    /// </summary>
    /// <param name="value">The name value.</param>
    /// <returns>A processor name.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is invalid.</exception>
    public static ProcessorName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Processor name cannot be empty", nameof(value));
        }

        if (value.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Processor name cannot exceed {MaxLength} characters",
                nameof(value));
        }

        return new ProcessorName(value.Trim());
    }

    /// <summary>
    /// Returns the string representation of the processor name.
    /// </summary>
    /// <returns>The name value.</returns>
    public override string ToString() => Value;
}
