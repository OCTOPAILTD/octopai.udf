namespace NiFiMetadataPlatform.Infrastructure.Persistence.OpenSearch;

/// <summary>
/// Nested column data stored inside a column document.
/// </summary>
public sealed class ColumnDocumentData
{
    public string? Name { get; set; }
    public string? DataType { get; set; }
    public string? NativeType { get; set; }
    public bool? IsNullable { get; set; }
    public bool? IsPrimaryKey { get; set; }
    public int? OrdinalPosition { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public string? Description { get; set; }

    /// <summary>Table FQN (for JDBC columns).</summary>
    public FqnWrapper? TableFqn { get; set; }

    /// <summary>Processor FQN (for NiFi columns).</summary>
    public FqnWrapper? ProcessorFqn { get; set; }
}

/// <summary>Wrapper for FQN value objects serialized as { "value": "..." }.</summary>
public sealed class FqnWrapper
{
    public string? Value { get; set; }
}

/// <summary>
/// OpenSearch document model for NiFi processors.
/// Contains all processor properties for search and retrieval.
/// </summary>
public sealed class ProcessorDocument
{
    /// <summary>
    /// Gets or sets the processor FQN (used as document ID).
    /// </summary>
    public string Fqn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processor name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processor type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent process group ID.
    /// </summary>
    public string ParentProcessGroupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processor status.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Gets or sets the processor description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the processor owner.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets the processor properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the processor tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the platform (always "NiFi").
    /// </summary>
    public string Platform { get; set; } = "NiFi";

    /// <summary>
    /// Gets or sets the parent container URN (for columns and tables).
    /// </summary>
    public string? ParentContainerUrn { get; set; }

    /// <summary>
    /// Gets or sets nested column data (populated for COLUMN type documents).
    /// </summary>
    public ColumnDocumentData? Column { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
