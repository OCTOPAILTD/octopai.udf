namespace NiFiMetadataPlatform.API.Services;

/// <summary>
/// Service for ingesting NiFi metadata into OpenSearch and ArangoDB.
/// </summary>
public interface INiFiMetadataIngestionService
{
    /// <summary>
    /// Ingest metadata from a NiFi container.
    /// </summary>
    /// <param name="containerId">The Docker container ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entities ingested.</returns>
    Task<int> IngestFromContainerAsync(string containerId, CancellationToken cancellationToken = default);
}
