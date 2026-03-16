using NiFiMetadataPlatform.Domain.Common;
using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Events;

/// <summary>
/// Domain event raised when a processor is deactivated.
/// </summary>
public sealed record ProcessorDeactivatedEvent : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorDeactivatedEvent"/> class.
    /// </summary>
    /// <param name="processorId">The processor ID.</param>
    /// <param name="fqn">The processor FQN.</param>
    public ProcessorDeactivatedEvent(ProcessorId processorId, ProcessorFqn fqn)
        : base()
    {
        ProcessorId = processorId;
        Fqn = fqn;
    }

    /// <summary>
    /// Gets the processor ID.
    /// </summary>
    public ProcessorId ProcessorId { get; }

    /// <summary>
    /// Gets the processor FQN.
    /// </summary>
    public ProcessorFqn Fqn { get; }
}
