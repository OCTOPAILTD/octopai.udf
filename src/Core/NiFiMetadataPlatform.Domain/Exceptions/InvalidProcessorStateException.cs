namespace NiFiMetadataPlatform.Domain.Exceptions;

/// <summary>
/// Exception thrown when a processor is in an invalid state for an operation.
/// </summary>
public sealed class InvalidProcessorStateException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidProcessorStateException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidProcessorStateException(string message)
        : base(message)
    {
    }
}
