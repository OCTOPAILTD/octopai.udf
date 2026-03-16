using NiFiMetadataPlatform.Application.Common;

namespace NiFiMetadataPlatform.Application.Commands.CreateProcessor;

/// <summary>
/// Command to create a new processor.
/// </summary>
public sealed record CreateProcessorCommand : ICommand<Result<ProcessorDto>>
{
    /// <summary>
    /// Gets the container ID.
    /// </summary>
    public required string ContainerId { get; init; }

    /// <summary>
    /// Gets the processor ID.
    /// </summary>
    public required string ProcessorId { get; init; }

    /// <summary>
    /// Gets the processor name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the processor type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the parent process group ID.
    /// </summary>
    public required string ParentProcessGroupId { get; init; }

    /// <summary>
    /// Gets the processor properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; init; } = new();

    /// <summary>
    /// Gets the processor description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the processor owner.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Gets the processor tags.
    /// </summary>
    public List<string> Tags { get; init; } = new();
}
