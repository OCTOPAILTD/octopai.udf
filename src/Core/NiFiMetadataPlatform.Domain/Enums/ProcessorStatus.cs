namespace NiFiMetadataPlatform.Domain.Enums;

/// <summary>
/// Represents the status of a NiFi processor.
/// </summary>
public enum ProcessorStatus
{
    /// <summary>
    /// Processor is active and running.
    /// </summary>
    Active,

    /// <summary>
    /// Processor is inactive/stopped.
    /// </summary>
    Inactive,

    /// <summary>
    /// Processor has been deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// Processor is in an invalid state.
    /// </summary>
    Invalid
}
