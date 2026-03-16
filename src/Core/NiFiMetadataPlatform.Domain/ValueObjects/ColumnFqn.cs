namespace NiFiMetadataPlatform.Domain.ValueObjects;

/// <summary>
/// Represents a Fully Qualified Name (FQN) for a NiFi processor column.
/// Format: nifi://container/{containerId}/processor/{processorId}/column/{columnName}
/// </summary>
public sealed record ColumnFqn
{
    private const string Prefix = "nifi://";
    private const string ContainerSegment = "container";
    private const string ProcessorSegment = "processor";
    private const string ColumnSegment = "column";

    /// <summary>
    /// Gets the FQN value.
    /// </summary>
    public string Value { get; }

    private ColumnFqn(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a column FQN from container, processor, and column name.
    /// </summary>
    /// <param name="containerId">The container ID.</param>
    /// <param name="processorId">The processor ID.</param>
    /// <param name="columnName">The column name.</param>
    /// <returns>A column FQN.</returns>
    /// <exception cref="ArgumentException">Thrown when IDs are invalid.</exception>
    public static ColumnFqn Create(string containerId, string processorId, string columnName)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentException("Container ID cannot be empty", nameof(containerId));
        }

        if (string.IsNullOrWhiteSpace(processorId))
        {
            throw new ArgumentException("Processor ID cannot be empty", nameof(processorId));
        }

        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ArgumentException("Column name cannot be empty", nameof(columnName));
        }

        var fqn = $"{Prefix}{ContainerSegment}/{containerId}/{ProcessorSegment}/{processorId}/{ColumnSegment}/{columnName}";
        return new ColumnFqn(fqn);
    }

    /// <summary>
    /// Creates a column FQN from a processor FQN and column name.
    /// </summary>
    /// <param name="processorFqn">The processor FQN.</param>
    /// <param name="columnName">The column name.</param>
    /// <returns>A column FQN.</returns>
    public static ColumnFqn CreateFromProcessor(ProcessorFqn processorFqn, string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ArgumentException("Column name cannot be empty", nameof(columnName));
        }

        return new ColumnFqn($"{processorFqn.Value}/{ColumnSegment}/{columnName}");
    }

    /// <summary>
    /// Parses a column FQN from a string.
    /// </summary>
    /// <param name="fqn">The FQN string.</param>
    /// <returns>A column FQN.</returns>
    /// <exception cref="ArgumentException">Thrown when the FQN format is invalid.</exception>
    public static ColumnFqn Parse(string fqn)
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
        if (parts.Length != 6 ||
            parts[0] != ContainerSegment ||
            parts[2] != ProcessorSegment ||
            parts[4] != ColumnSegment)
        {
            throw new ArgumentException(
                $"Invalid FQN format. Expected: {Prefix}{ContainerSegment}/{{containerId}}/{ProcessorSegment}/{{processorId}}/{ColumnSegment}/{{columnName}}",
                nameof(fqn));
        }

        return new ColumnFqn(fqn);
    }

    /// <summary>
    /// Tries to parse a column FQN from a string.
    /// </summary>
    /// <param name="fqn">The FQN string.</param>
    /// <param name="columnFqn">The parsed column FQN.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string fqn, out ColumnFqn? columnFqn)
    {
        columnFqn = null;

        try
        {
            columnFqn = Parse(fqn);
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
    /// Extracts the column name from the FQN.
    /// </summary>
    /// <returns>The column name.</returns>
    public string GetColumnName()
    {
        var parts = Value[Prefix.Length..].Split('/');
        return parts[5];
    }

    /// <summary>
    /// Gets the parent processor FQN.
    /// </summary>
    /// <returns>The processor FQN.</returns>
    public ProcessorFqn GetProcessorFqn()
    {
        var parts = Value[Prefix.Length..].Split('/');
        var containerId = parts[1];
        var processorId = parts[3];
        return ProcessorFqn.Create(containerId, processorId);
    }

    /// <summary>
    /// Returns the string representation of the FQN.
    /// </summary>
    /// <returns>The FQN value.</returns>
    public override string ToString() => Value;
}
