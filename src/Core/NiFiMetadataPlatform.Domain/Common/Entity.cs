namespace NiFiMetadataPlatform.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Gets the domain events raised by this entity.
    /// </summary>
    /// <returns>A read-only collection of domain events.</returns>
    public IReadOnlyCollection<DomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    /// <summary>
    /// Clears all domain events.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Adds a domain event to the entity.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Checks equality based on entity ID.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <summary>
    /// Gets the hash code based on entity ID.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
