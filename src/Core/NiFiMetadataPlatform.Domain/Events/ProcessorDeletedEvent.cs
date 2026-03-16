using NiFiMetadataPlatform.Domain.Common;
using NiFiMetadataPlatform.Domain.ValueObjects;

namespace NiFiMetadataPlatform.Domain.Events;

/// <summary>
/// Domain event raised when a processor is deleted.
/// </summary>
public sealed record ProcessorDeletedEvent : DomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorDeletedEvent"/> class.
    /// </summary>
    /// <param name="processorId">The processor ID.</param>
    /// <param name="fqn">The processor FQN.</param>
    public ProcessorDeletedEvent(ProcessorId processorId, ProcessorFqn fqn)
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
