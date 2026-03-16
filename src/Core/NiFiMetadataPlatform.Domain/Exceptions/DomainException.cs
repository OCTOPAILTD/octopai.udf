namespace NiFiMetadataPlatform.Domain.Exceptions;

/// <summary>
/// Base exception for domain-related errors.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    protected DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
