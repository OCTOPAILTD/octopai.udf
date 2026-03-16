using NiFiMetadataPlatform.Application.Common;

namespace NiFiMetadataPlatform.Application.Queries.GetProcessor;

/// <summary>
/// Query to get a processor by FQN.
/// </summary>
public sealed record GetProcessorQuery : IQuery<Result<ProcessorDto?>>
{
    /// <summary>
    /// Gets the processor FQN.
    /// </summary>
    public required string Fqn { get; init; }
}
