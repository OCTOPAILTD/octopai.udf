using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NiFiMetadataPlatform.API.Services;

/// <summary>
/// Service for extracting column schemas from NiFi processor properties.
/// </summary>
public interface INiFiSchemaExtractor
{
    /// <summary>
    /// Extracts columns from Avro schema JSON.
    /// </summary>
    /// <param name="avroSchemaJson">The Avro schema JSON string.</param>
    /// <returns>List of extracted columns.</returns>
    List<SchemaColumn> ExtractFromAvroSchema(string avroSchemaJson);

    /// <summary>
    /// Extracts columns from JSON schema.
    /// </summary>
    /// <param name="jsonSchema">The JSON schema string.</param>
    /// <returns>List of extracted columns.</returns>
    List<SchemaColumn> ExtractFromJsonSchema(string jsonSchema);

    /// <summary>
    /// Extracts columns from NiFi processor properties.
    /// </summary>
    /// <param name="processorProperties">The processor properties dictionary.</param>
    /// <returns>List of extracted columns.</returns>
    List<SchemaColumn> ExtractFromProcessorProperties(Dictionary<string, string> processorProperties);
}

/// <summary>
/// Represents a column extracted from a schema.
/// </summary>
public sealed class SchemaColumn
{
    public string Name { get; set; } = string.Empty;
    public string? DataType { get; set; }
    public bool? IsNullable { get; set; }
    public string? Description { get; set; }
    public int? OrdinalPosition { get; set; }
}

/// <summary>
/// Implementation of NiFi schema extractor.
/// </summary>
public sealed class NiFiSchemaExtractor : INiFiSchemaExtractor
{
    private readonly ILogger<NiFiSchemaExtractor> _logger;

    public NiFiSchemaExtractor(ILogger<NiFiSchemaExtractor> logger)
    {
        _logger = logger;
    }

    public List<SchemaColumn> ExtractFromAvroSchema(string avroSchemaJson)
    {
        var columns = new List<SchemaColumn>();

        try
        {
            if (string.IsNullOrWhiteSpace(avroSchemaJson))
            {
                return columns;
            }

            var schemaDoc = JsonDocument.Parse(avroSchemaJson);
            var root = schemaDoc.RootElement;

            // Avro schema format: { "type": "record", "fields": [...] }
            if (root.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Array)
            {
                int position = 0;
                foreach (var field in fieldsElement.EnumerateArray())
                {
                    var column = new SchemaColumn
                    {
                        OrdinalPosition = position++
                    };

                    // Extract field name
                    if (field.TryGetProperty("name", out var nameElement))
                    {
                        column.Name = nameElement.GetString() ?? string.Empty;
                    }

                    // Extract field type
                    if (field.TryGetProperty("type", out var typeElement))
                    {
                        column.DataType = ExtractAvroType(typeElement, out var isNullable);
                        column.IsNullable = isNullable;
                    }

                    // Extract description/doc
                    if (field.TryGetProperty("doc", out var docElement))
                    {
                        column.Description = docElement.GetString();
                    }

                    if (!string.IsNullOrEmpty(column.Name))
                    {
                        columns.Add(column);
                    }
                }

                _logger.LogDebug("Extracted {Count} columns from Avro schema", columns.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Avro schema");
        }

        return columns;
    }

    public List<SchemaColumn> ExtractFromJsonSchema(string jsonSchema)
    {
        var columns = new List<SchemaColumn>();

        try
        {
            if (string.IsNullOrWhiteSpace(jsonSchema))
            {
                return columns;
            }

            var schemaDoc = JsonDocument.Parse(jsonSchema);
            var root = schemaDoc.RootElement;

            // JSON Schema format: { "properties": { "column1": {...}, "column2": {...} } }
            if (root.TryGetProperty("properties", out var propertiesElement) && propertiesElement.ValueKind == JsonValueKind.Object)
            {
                int position = 0;
                foreach (var property in propertiesElement.EnumerateObject())
                {
                    var column = new SchemaColumn
                    {
                        Name = property.Name,
                        OrdinalPosition = position++
                    };

                    var propertyValue = property.Value;

                    // Extract type
                    if (propertyValue.TryGetProperty("type", out var typeElement))
                    {
                        column.DataType = typeElement.GetString();
                    }

                    // Extract description
                    if (propertyValue.TryGetProperty("description", out var descElement))
                    {
                        column.Description = descElement.GetString();
                    }

                    // Check if nullable (JSON Schema uses "type": ["string", "null"])
                    if (propertyValue.TryGetProperty("type", out var typeCheck) && typeCheck.ValueKind == JsonValueKind.Array)
                    {
                        var types = typeCheck.EnumerateArray().Select(t => t.GetString()).ToList();
                        column.IsNullable = types.Contains("null");
                        column.DataType = types.FirstOrDefault(t => t != "null");
                    }

                    columns.Add(column);
                }

                _logger.LogDebug("Extracted {Count} columns from JSON schema", columns.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse JSON schema");
        }

        return columns;
    }

    public List<SchemaColumn> ExtractFromProcessorProperties(Dictionary<string, string> processorProperties)
    {
        var columns = new List<SchemaColumn>();

        try
        {
            // Check for Avro schema in properties
            if (processorProperties.TryGetValue("avro.schema", out var avroSchema) && !string.IsNullOrWhiteSpace(avroSchema))
            {
                columns.AddRange(ExtractFromAvroSchema(avroSchema));
            }

            // Check for schema.name or schema.text properties
            if (processorProperties.TryGetValue("schema.text", out var schemaText) && !string.IsNullOrWhiteSpace(schemaText))
            {
                // Try to parse as Avro first, then JSON
                var avroColumns = ExtractFromAvroSchema(schemaText);
                if (avroColumns.Any())
                {
                    columns.AddRange(avroColumns);
                }
                else
                {
                    columns.AddRange(ExtractFromJsonSchema(schemaText));
                }
            }

            // Check for Record Schema property (used by ConvertRecord processors)
            if (processorProperties.TryGetValue("record-schema", out var recordSchema) && !string.IsNullOrWhiteSpace(recordSchema))
            {
                columns.AddRange(ExtractFromAvroSchema(recordSchema));
            }

            _logger.LogDebug("Extracted {Count} columns from processor properties", columns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract columns from processor properties");
        }

        return columns;
    }

    private string ExtractAvroType(JsonElement typeElement, out bool isNullable)
    {
        isNullable = false;

        // Avro type can be a string or an array (union type)
        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString() ?? "string";
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            // Union type: ["null", "string"] means nullable string
            var types = typeElement.EnumerateArray().Select(t => t.GetString()).ToList();
            isNullable = types.Contains("null");
            return types.FirstOrDefault(t => t != "null") ?? "string";
        }

        if (typeElement.ValueKind == JsonValueKind.Object)
        {
            // Complex type: { "type": "record", "name": "..." }
            if (typeElement.TryGetProperty("type", out var innerType))
            {
                return innerType.GetString() ?? "record";
            }
        }

        return "string";
    }
}
