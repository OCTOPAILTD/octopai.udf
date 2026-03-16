namespace NiFiMetadataPlatform.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for a processor.
/// </summary>
public sealed record ProcessorId
{
    /// <summary>
    /// Gets the GUID value.
    /// </summary>
    public Guid Value { get; }

    private ProcessorId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new processor ID.
    /// </summary>
    /// <returns>A new processor ID.</returns>
    public static ProcessorId CreateNew() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a processor ID from a GUID.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    /// <returns>A processor ID.</returns>
    public static ProcessorId From(Guid value) => new(value);

    /// <summary>
    /// Parses a processor ID from a string.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A processor ID.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid GUID.</exception>
    public static ProcessorId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Processor ID cannot be empty", nameof(value));
        }

        if (!Guid.TryParse(value, out var guid))
        {
            throw new ArgumentException($"Invalid processor ID format: {value}", nameof(value));
        }

        return new ProcessorId(guid);
    }

    /// <summary>
    /// Tries to parse a processor ID from a string.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="processorId">The parsed processor ID.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string value, out ProcessorId? processorId)
    {
        processorId = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!Guid.TryParse(value, out var guid))
        {
            return false;
        }

        processorId = new ProcessorId(guid);
        return true;
    }

    /// <summary>
    /// Returns the string representation of the processor ID.
    /// </summary>
    /// <returns>The GUID as a string.</returns>
    public override string ToString() => Value.ToString();
}
