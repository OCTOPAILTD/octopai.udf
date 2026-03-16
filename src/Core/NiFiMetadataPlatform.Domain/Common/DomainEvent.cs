namespace NiFiMetadataPlatform.Domain.Common;

/// <summary>
/// Base class for all domain events.
/// </summary>
public abstract record DomainEvent(DateTime OccurredAt)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class with the current UTC time.
    /// </summary>
    protected DomainEvent()
        : this(DateTime.UtcNow)
    {
    }
}
