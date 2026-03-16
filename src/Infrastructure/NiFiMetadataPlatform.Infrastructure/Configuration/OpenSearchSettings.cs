namespace NiFiMetadataPlatform.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for OpenSearch.
/// </summary>
public sealed class OpenSearchSettings
{
    /// <summary>
    /// Gets or sets the OpenSearch connection URLs.
    /// </summary>
    public List<string> Urls { get; set; } = new() { "http://localhost:9200" };

    /// <summary>
    /// Gets or sets the index name.
    /// </summary>
    public string IndexName { get; set; } = "nifi-processors";

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of connection retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the number of shards.
    /// </summary>
    public int NumberOfShards { get; set; } = 3;

    /// <summary>
    /// Gets or sets the number of replicas.
    /// </summary>
    public int NumberOfReplicas { get; set; } = 2;
}
