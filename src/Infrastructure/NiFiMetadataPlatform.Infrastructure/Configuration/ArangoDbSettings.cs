namespace NiFiMetadataPlatform.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for ArangoDB.
/// </summary>
public sealed class ArangoDbSettings
{
    /// <summary>
    /// Gets or sets the ArangoDB endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:8529";

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string Database { get; set; } = "nifi_metadata";

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = "root";

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = "rootpassword";

    /// <summary>
    /// Gets or sets the lineage collection name.
    /// </summary>
    public string LineageCollection { get; set; } = "lineage";

    /// <summary>
    /// Gets or sets the vertex collection name.
    /// </summary>
    public string VertexCollection { get; set; } = "vertices";
}
