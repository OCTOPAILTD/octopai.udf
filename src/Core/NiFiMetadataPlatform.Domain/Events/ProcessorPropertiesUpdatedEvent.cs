using NiFiMetadataPlatform.Domain.Common;
using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Events;

/// <summary>
/// Domain event raised when processor properties are updated.
/// </summary>
public sealed record ProcessorPropertiesUpdatedEvent : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorPropertiesUpdatedEvent"/> class.
    /// </summary>
    /// <param name="processorId">The processor ID.</param>
    /// <param name="fqn">The processor FQN.</param>
    /// <param name="properties">The updated properties.</param>
    public ProcessorPropertiesUpdatedEvent(
        ProcessorId processorId,
        ProcessorFqn fqn,
        ProcessorProperties properties)
        : base()
    {
        ProcessorId = processorId;
        Fqn = fqn;
        Properties = properties;
    }

    /// <summary>
    /// Gets the processor ID.
    /// </summary>
    public ProcessorId ProcessorId { get; }

    /// <summary>
    /// Gets the processor FQN.
    /// </summary>
    public ProcessorFqn Fqn { get; }

    /// <summary>
    /// Gets the updated properties.
    /// </summary>
    public ProcessorProperties Properties { get; }
}
