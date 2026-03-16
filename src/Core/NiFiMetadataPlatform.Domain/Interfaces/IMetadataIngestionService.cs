namespace NiFiMetadataPlatform.Domain.Interfaces;

/// <summary>
/// Interface for metadata ingestion services.
/// Provides a pluggable architecture for ingesting metadata from various data platforms.
/// </summary>
/// <remarks>
/// This interface enables extensibility for future data tools such as:
/// - Apache NiFi (current implementation)
/// - Trino
/// - Apache Kafka
/// - Apache Hive
/// - Apache Impala
/// - Databricks
/// - And other data platforms
/// 
/// Each implementation should handle:
/// 1. Connecting to the source platform
/// 2. Discovering metadata changes
/// 3. Transforming platform-specific metadata to a common format
/// 4. Sending metadata to the central API
/// </remarks>
public interface IMetadataIngestionService
{
    /// <summary>
    /// Gets the name of the platform this service ingests from.
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// Gets the version of the platform API this service supports.
    /// </summary>
    string SupportedVersion { get; }

    /// <summary>
    /// Starts the metadata ingestion process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the ingestion.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the metadata ingestion process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Performs a one-time metadata discovery and ingestion.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the number of entities discovered.</returns>
    Task<int> DiscoverMetadataAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the connection to the source platform is healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the health status.</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
}
