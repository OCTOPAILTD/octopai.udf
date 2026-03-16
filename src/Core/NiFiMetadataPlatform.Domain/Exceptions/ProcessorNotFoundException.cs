namespace NiFiMetadataPlatform.Domain.Exceptions;

/// <summary>
/// Exception thrown when a processor is not found.
/// </summary>
public sealed class ProcessorNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessorNotFoundException"/> class.
    /// </summary>
    /// <param name="fqn">The processor FQN that was not found.</param>
    public ProcessorNotFoundException(string fqn)
        : base($"Processor not found: {fqn}")
    {
        Fqn = fqn;
    }

    /// <summary>
    /// Gets the FQN of the processor that was not found.
    /// </summary>
    public string Fqn { get; }
}
