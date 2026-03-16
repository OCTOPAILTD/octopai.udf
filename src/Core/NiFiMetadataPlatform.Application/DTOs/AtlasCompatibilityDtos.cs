namespace NiFiMetadataPlatform.Application.DTOs;

/// <summary>
/// Atlas-compatible search response.
/// </summary>
public sealed class AtlasSearchResponse
{
    public List<AtlasEntityDto> Results { get; set; } = new();
    public int Total { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Atlas-compatible entity DTO.
/// </summary>
public sealed class AtlasEntityDto
{
    public string Urn { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? ParentContainerUrn { get; set; }
    public List<string>? Columns { get; set; }
}

/// <summary>
/// Atlas-compatible lineage response.
/// </summary>
public sealed class AtlasLineageResponse
{
    public List<AtlasEntityDto> Upstream { get; set; } = new();
    public List<AtlasEntityDto> Downstream { get; set; } = new();
    public List<ColumnLineageDto> ColumnLineage { get; set; } = new();
}

/// <summary>
/// Column-level lineage information.
/// </summary>
public sealed class ColumnLineageDto
{
    public string FromUrn { get; set; } = string.Empty;
    public string FromColumn { get; set; } = string.Empty;
    public string ToUrn { get; set; } = string.Empty;
    public string ToColumn { get; set; } = string.Empty;
}

/// <summary>
/// Atlas-compatible platform statistics.
/// </summary>
public sealed class AtlasPlatformStatsResponse
{
    public List<PlatformStatDto> Platforms { get; set; } = new();
}

/// <summary>
/// Platform statistics DTO.
/// </summary>
public sealed class PlatformStatDto
{
    public string Platform { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Atlas-compatible hierarchy response.
/// </summary>
public sealed class AtlasHierarchyResponse
{
    public List<AtlasEntityDto> Containers { get; set; } = new();
}

/// <summary>
/// Atlas-compatible children response.
/// </summary>
public sealed class AtlasChildrenResponse
{
    public List<AtlasEntityDto> Children { get; set; } = new();
    public int Total { get; set; }
}

/// <summary>
/// Lightweight column info for table detail pages.
/// </summary>
public sealed class ColumnInfoDto
{
    public string Urn { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string NativeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
