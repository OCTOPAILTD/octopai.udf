namespace NiFiMetadataPlatform.Domain.ValueObjects;

/// <summary>
/// Represents a Fully Qualified Name (FQN) for a NiFi processor.
/// Format: nifi://container/{containerId}/processor/{processorId}
/// </summary>
public sealed record ProcessorFqn
{
    private const string Prefix = "nifi://";
    private const string ContainerSegment = "container";
    private const string ProcessorSegment = "processor";

    /// <summary>
    /// Gets the FQN value.
    /// </summary>
    public string Value { get; }

    private ProcessorFqn(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a processor FQN from container and processor IDs.
    /// </summary>
    /// <param name="containerId">The container ID.</param>
    /// <param name="processorId">The processor ID.</param>
    /// <returns>A processor FQN.</returns>
    /// <exception cref="ArgumentException">Thrown when IDs are invalid.</exception>
    public static ProcessorFqn Create(string containerId, string processorId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentException("Container ID cannot be empty", nameof(containerId));
        }

        if (string.IsNullOrWhiteSpace(processorId))
        {
            throw new ArgumentException("Processor ID cannot be empty", nameof(processorId));
        }

        var fqn = $"{Prefix}{ContainerSegment}/{containerId}/{ProcessorSegment}/{processorId}";
        return new ProcessorFqn(fqn);
    }

    /// <summary>
    /// Parses a processor FQN from a string.
    /// </summary>
    /// <param name="fqn">The FQN string.</param>
    /// <returns>A processor FQN.</returns>
    /// <exception cref="ArgumentException">Thrown when the FQN format is invalid.</exception>
    public static ProcessorFqn Parse(string fqn)
    {
        if (string.IsNullOrWhiteSpace(fqn))
        {
            throw new ArgumentException("FQN cannot be empty", nameof(fqn));
        }

        if (!fqn.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"FQN must start with {Prefix}", nameof(fqn));
        }

        var parts = fqn[Prefix.Length..].Split('/');
        if (parts.Length != 4 ||
            parts[0] != ContainerSegment ||
            parts[2] != ProcessorSegment)
        {
            throw new ArgumentException(
                $"Invalid FQN format. Expected: {Prefix}{ContainerSegment}/{{containerId}}/{ProcessorSegment}/{{processorId}}",
                nameof(fqn));
        }

        return new ProcessorFqn(fqn);
    }

    /// <summary>
    /// Tries to parse a processor FQN from a string.
    /// </summary>
    /// <param name="fqn">The FQN string.</param>
    /// <param name="processorFqn">The parsed processor FQN.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string fqn, out ProcessorFqn? processorFqn)
    {
        processorFqn = null;

        try
        {
            processorFqn = Parse(fqn);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts the container ID from the FQN.
    /// </summary>
    /// <returns>The container ID.</returns>
    public string GetContainerId()
    {
        var parts = Value[Prefix.Length..].Split('/');
        return parts[1];
    }

    /// <summary>
    /// Extracts the processor ID from the FQN.
    /// </summary>
    /// <returns>The processor ID.</returns>
    public string GetProcessorId()
    {
        var parts = Value[Prefix.Length..].Split('/');
        return parts[3];
    }

    /// <summary>
    /// Returns the string representation of the FQN.
    /// </summary>
    /// <returns>The FQN value.</returns>
    public override string ToString() => Value;
}
