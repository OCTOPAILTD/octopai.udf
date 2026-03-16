namespace NiFiMetadataPlatform.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for a process group.
/// </summary>
public sealed record ProcessGroupId
{
    /// <summary>
    /// Gets the ID value.
    /// </summary>
    public string Value { get; }

    private ProcessGroupId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Parses a process group ID from a string.
    /// </summary>
    /// <param name="value">The ID string.</param>
    /// <returns>A process group ID.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is invalid.</exception>
    public static ProcessGroupId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Process group ID cannot be empty", nameof(value));
        }

        return new ProcessGroupId(value.Trim());
    }

    /// <summary>
    /// Creates the root process group ID.
    /// </summary>
    /// <returns>The root process group ID.</returns>
    public static ProcessGroupId Root() => new("root");

    /// <summary>
    /// Checks if this is the root process group.
    /// </summary>
    /// <returns>True if root, false otherwise.</returns>
    public bool IsRoot() => Value.Equals("root", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the string representation of the process group ID.
    /// </summary>
    /// <returns>The ID value.</returns>
    public override string ToString() => Value;
}
