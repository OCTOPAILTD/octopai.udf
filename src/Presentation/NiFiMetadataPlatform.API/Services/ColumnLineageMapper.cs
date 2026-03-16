using Microsoft.Extensions.Logging;

namespace NiFiMetadataPlatform.API.Services;

/// <summary>
/// Service for mapping column lineage between processors.
/// </summary>
public interface IColumnLineageMapper
{
    /// <summary>
    /// Maps columns between two processors based on processor types and schemas.
    /// </summary>
    /// <param name="sourceProcessorType">The source processor type.</param>
    /// <param name="targetProcessorType">The target processor type.</param>
    /// <param name="sourceColumns">The source columns.</param>
    /// <param name="targetColumns">The target columns.</param>
    /// <returns>List of column-to-column mappings.</returns>
    List<ColumnMapping> MapColumns(
        string sourceProcessorType,
        string targetProcessorType,
        List<SchemaColumn> sourceColumns,
        List<SchemaColumn> targetColumns);
}

/// <summary>
/// Represents a column-to-column mapping.
/// </summary>
public sealed class ColumnMapping
{
    public string SourceColumnName { get; set; } = string.Empty;
    public string TargetColumnName { get; set; } = string.Empty;
    public string TransformationType { get; set; } = "DIRECT"; // DIRECT, TRANSFORM, AGGREGATE, etc.
}

/// <summary>
/// Implementation of column lineage mapper.
/// </summary>
public sealed class ColumnLineageMapper : IColumnLineageMapper
{
    private readonly ILogger<ColumnLineageMapper> _logger;

    public ColumnLineageMapper(ILogger<ColumnLineageMapper> logger)
    {
        _logger = logger;
    }

    public List<ColumnMapping> MapColumns(
        string sourceProcessorType,
        string targetProcessorType,
        List<SchemaColumn> sourceColumns,
        List<SchemaColumn> targetColumns)
    {
        var mappings = new List<ColumnMapping>();

        try
        {
            // Strategy 1: Direct name matching (most common case)
            // Columns with the same name are assumed to be the same
            foreach (var sourceColumn in sourceColumns)
            {
                var matchingTarget = targetColumns.FirstOrDefault(
                    t => t.Name.Equals(sourceColumn.Name, StringComparison.OrdinalIgnoreCase));

                if (matchingTarget != null)
                {
                    mappings.Add(new ColumnMapping
                    {
                        SourceColumnName = sourceColumn.Name,
                        TargetColumnName = matchingTarget.Name,
                        TransformationType = DetermineTransformationType(sourceProcessorType, targetProcessorType)
                    });
                }
            }

            // Strategy 2: Positional matching (if names don't match but counts are equal)
            if (mappings.Count == 0 && sourceColumns.Count == targetColumns.Count)
            {
                _logger.LogDebug(
                    "No name matches found, using positional matching for {Count} columns",
                    sourceColumns.Count);

                for (int i = 0; i < sourceColumns.Count; i++)
                {
                    mappings.Add(new ColumnMapping
                    {
                        SourceColumnName = sourceColumns[i].Name,
                        TargetColumnName = targetColumns[i].Name,
                        TransformationType = "TRANSFORM"
                    });
                }
            }

            _logger.LogDebug(
                "Mapped {MappingCount} columns between {SourceType} and {TargetType}",
                mappings.Count,
                sourceProcessorType,
                targetProcessorType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping columns between processors");
        }

        return mappings;
    }

    private string DetermineTransformationType(string sourceProcessorType, string targetProcessorType)
    {
        // Determine transformation type based on processor types
        var transformProcessors = new[]
        {
            "ConvertAvroToJSON",
            "ConvertJSONToAvro",
            "ConvertRecord",
            "JoltTransformJSON",
            "UpdateAttribute",
            "EvaluateJsonPath"
        };

        if (transformProcessors.Any(p => targetProcessorType.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            return "TRANSFORM";
        }

        // Pass-through processors (no transformation)
        var passThroughProcessors = new[]
        {
            "RouteOnAttribute",
            "RouteOnContent",
            "DistributeLoad",
            "MergeContent"
        };

        if (passThroughProcessors.Any(p => targetProcessorType.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            return "DIRECT";
        }

        // Default to DIRECT for most cases
        return "DIRECT";
    }
}
