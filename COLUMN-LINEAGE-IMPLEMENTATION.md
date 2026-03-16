# Column-Level Lineage Implementation Summary

## ✅ Completed Components

### 1. Domain Entities & Value Objects

**Created Files:**
- `src/Core/NiFiMetadataPlatform.Domain/ValueObjects/ColumnFqn.cs`
  - URN format: `nifi://container/{containerId}/processor/{processorId}/column/{columnName}`
  - Methods: Create, Parse, GetContainerId, GetProcessorId, GetColumnName, GetProcessorFqn

- `src/Core/NiFiMetadataPlatform.Domain/ValueObjects/DatabaseFqn.cs`
  - URN formats:
    - Database: `jdbc://{server}/{database}`
    - Schema: `jdbc://{server}/{database}/{schema}`
    - Table: `jdbc://{server}/{database}/{schema}/{table}`
    - Column: `jdbc://{server}/{database}/{schema}/{table}/column/{columnName}`
  - Methods: CreateDatabase, CreateSchema, CreateTable, CreateColumn, GetParentFqn

- `src/Core/NiFiMetadataPlatform.Domain/Entities/NiFiColumn.cs`
  - Properties: Fqn, Name, ProcessorFqn, DataType, Description, IsNullable, OrdinalPosition

- `src/Core/NiFiMetadataPlatform.Domain/Entities/DatabaseTable.cs`
  - Properties: Fqn, Name, Server, Database, Schema, Platform, Description, TableType

- `src/Core/NiFiMetadataPlatform.Domain/Entities/DatabaseColumn.cs`
  - Properties: Fqn, Name, TableFqn, DataType, IsNullable, IsPrimaryKey, OrdinalPosition

### 2. Services

**Created Files:**
- `src/Presentation/NiFiMetadataPlatform.API/Services/GSPSqlParserService.cs`
  - Calls GSP API at `http://gsp.onprem.qa.octopai-corp.local/parser/gsp/dataflow`
  - Parses SQL queries to extract column-level lineage
  - Returns: SourceTables, ResultColumns, ColumnLineages

- `src/Presentation/NiFiMetadataPlatform.API/Services/NiFiSchemaExtractor.cs`
  - Extracts columns from Avro schema JSON
  - Extracts columns from JSON schema
  - Extracts columns from processor properties (avro.schema, schema.text, record-schema)

- `src/Presentation/NiFiMetadataPlatform.API/Services/ColumnLineageMapper.cs`
  - Maps columns between processors by name (primary strategy)
  - Falls back to positional matching if names don't match
  - Determines transformation type (DIRECT, TRANSFORM)

### 3. Repository Interfaces & Implementations

**Updated Files:**
- `src/Core/NiFiMetadataPlatform.Application/Interfaces/IGraphRepository.cs`
  - Added: `AddColumnVertexAsync(object column, string fqn)`
  - Added: `AddTableVertexAsync(DatabaseTable table)`
  - Added: `AddColumnLineageEdgeAsync(string fromColumnFqn, string toColumnFqn, RelationshipType)`

- `src/Core/NiFiMetadataPlatform.Application/Interfaces/ISearchRepository.cs`
  - Added: `IndexColumnAsync(object column, string fqn, string entityType, string platform, string parentFqn)`
  - Added: `IndexTableAsync(DatabaseTable table)`
  - Added: `IndexDatabaseEntityAsync(string fqn, string name, string entityType, string platform, string parentFqn, Dictionary properties)`

- `src/Infrastructure/NiFiMetadataPlatform.Infrastructure/Persistence/ArangoDB/ArangoDbRepository.cs`
  - Added collections: `columns`, `tables`, `column_lineage`
  - Implemented upsert with `POST` + `Overwrite=true`
  - Implemented: `AddColumnVertexAsync`, `AddTableVertexAsync`, `AddColumnLineageEdgeAsync`

- `src/Infrastructure/NiFiMetadataPlatform.Infrastructure/Persistence/OpenSearch/OpenSearchRepository.cs`
  - Implemented: `IndexColumnAsync`, `IndexTableAsync`, `IndexDatabaseEntityAsync`
  - All use document ID for automatic upsert

### 4. Service Registration

**Updated File:**
- `src/Presentation/NiFiMetadataPlatform.API/Program.cs`
  - Registered: `IGSPSqlParserService`
  - Registered: `INiFiSchemaExtractor`
  - Registered: `IColumnLineageMapper`

## 🔄 Pending Integration

### NiFiMetadataIngestionService Enhancement

**File to Update:** `src/Presentation/NiFiMetadataPlatform.API/Services/NiFiMetadataIngestionService.cs`

**Required Changes:**

1. **Inject New Services:**
```csharp
private readonly IGSPSqlParserService _gspParser;
private readonly INiFiSchemaExtractor _schemaExtractor;
private readonly IColumnLineageMapper _columnMapper;
```

2. **In `IngestProcessorAsync` Method:**

   a. **Extract Columns from Processor:**
   ```csharp
   // After creating processor entity
   var columns = new List<SchemaColumn>();
   
   // Try schema extraction first
   columns.AddRange(_schemaExtractor.ExtractFromProcessorProperties(properties));
   
   // For ExecuteSQL/PutDatabaseRecord, use GSP to parse SQL
   if (type.Contains("ExecuteSQL") || type.Contains("PutDatabaseRecord"))
   {
       var sqlQuery = properties.GetValueOrDefault("SQL select query") 
                   ?? properties.GetValueOrDefault("Table Name");
       
       if (!string.IsNullOrEmpty(sqlQuery))
       {
           var vendor = DetermineVendor(properties); // e.g., "dbvmssql"
           var gspResult = await _gspParser.ParseSqlAsync(sqlQuery, vendor, cancellationToken);
           
           if (gspResult != null)
           {
               // Create database table entities
               foreach (var table in gspResult.SourceTables)
               {
                   await IngestDatabaseTableAsync(table, cancellationToken);
               }
               
               // Extract columns from GSP result
               columns.AddRange(gspResult.ResultColumns.Select(c => new SchemaColumn 
               { 
                   Name = c.Name 
               }));
           }
       }
   }
   ```

   b. **Index Columns in OpenSearch:**
   ```csharp
   foreach (var column in columns)
   {
       var columnFqn = ColumnFqn.CreateFromProcessor(fqn, column.Name);
       var nifiColumn = NiFiColumn.Create(
           columnFqn,
           column.Name,
           fqn,
           column.DataType,
           column.Description,
           column.IsNullable,
           column.OrdinalPosition);
       
       await _searchRepository.IndexColumnAsync(
           nifiColumn,
           columnFqn.Value,
           "COLUMN",
           "NiFi",
           fqn.Value,
           cancellationToken);
       
       await _graphRepository.AddColumnVertexAsync(
           nifiColumn,
           columnFqn.Value,
           cancellationToken);
   }
   ```

3. **In `IngestConnectionAsync` Method:**

   a. **Build Column-Level Lineage:**
   ```csharp
   // After creating processor-level lineage edge
   
   // Get columns from source and target processors
   var sourceColumns = await GetProcessorColumnsAsync(sourceFqn, cancellationToken);
   var targetColumns = await GetProcessorColumnsAsync(targetFqn, cancellationToken);
   
   // Map columns
   var columnMappings = _columnMapper.MapColumns(
       sourceProcessorType,
       targetProcessorType,
       sourceColumns,
       targetColumns);
   
   // Create column lineage edges
   foreach (var mapping in columnMappings)
   {
       var sourceColumnFqn = ColumnFqn.CreateFromProcessor(sourceFqn, mapping.SourceColumnName);
       var targetColumnFqn = ColumnFqn.CreateFromProcessor(targetFqn, mapping.TargetColumnName);
       
       await _graphRepository.AddColumnLineageEdgeAsync(
           sourceColumnFqn.Value,
           targetColumnFqn.Value,
           RelationshipType.Lineage,
           cancellationToken);
   }
   ```

4. **New Helper Method:**
```csharp
private async Task IngestDatabaseTableAsync(
    GSPTable gspTable,
    CancellationToken cancellationToken)
{
    var server = "localhost"; // Parse from JDBC URL
    var database = gspTable.Database ?? "unknown";
    var schema = gspTable.Schema ?? "dbo";
    var tableName = gspTable.Name;
    
    // Create database entity
    var dbFqn = DatabaseFqn.CreateDatabase(server, database);
    await _searchRepository.IndexDatabaseEntityAsync(
        dbFqn.Value,
        database,
        "DATABASE",
        "MSSQL",
        null,
        new Dictionary<string, string> { ["server"] = server },
        cancellationToken);
    
    // Create schema entity
    var schemaFqn = DatabaseFqn.CreateSchema(server, database, schema);
    await _searchRepository.IndexDatabaseEntityAsync(
        schemaFqn.Value,
        schema,
        "SCHEMA",
        "MSSQL",
        dbFqn.Value,
        new Dictionary<string, string> { ["database"] = database },
        cancellationToken);
    
    // Create table entity
    var tableFqn = DatabaseFqn.CreateTable(server, database, schema, tableName);
    var table = DatabaseTable.Create(
        tableFqn,
        tableName,
        server,
        database,
        schema,
        "MSSQL");
    
    await _searchRepository.IndexTableAsync(table, cancellationToken);
    await _graphRepository.AddTableVertexAsync(table, cancellationToken);
    
    // Create column entities
    foreach (var columnName in gspTable.Columns)
    {
        var columnFqn = DatabaseFqn.CreateColumn(server, database, schema, tableName, columnName);
        var column = DatabaseColumn.Create(
            columnFqn,
            columnName,
            tableFqn,
            "VARCHAR",
            true,
            0);
        
        await _searchRepository.IndexColumnAsync(
            column,
            columnFqn.Value,
            "COLUMN",
            "MSSQL",
            tableFqn.Value,
            cancellationToken);
        
        await _graphRepository.AddColumnVertexAsync(
            column,
            columnFqn.Value,
            cancellationToken);
    }
}
```

## 📊 Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    NiFi Container                                │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐    │
│  │  ExecuteSQL  │────▶│ ConvertAvro  │────▶│ PutDatabase  │    │
│  │  (Processor) │     │  (Processor) │     │  (Processor) │    │
│  └──────────────┘     └──────────────┘     └──────────────┘    │
│         │                     │                     │            │
│         ▼                     ▼                     ▼            │
│    [product_id]          [product_id]          [product_id]     │
│    [product_name]        [product_name]        [product_name]   │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│            NiFiMetadataIngestionService                          │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐   │
│  │ GSPSqlParser   │  │ SchemaExtractor│  │ ColumnMapper   │   │
│  └────────────────┘  └────────────────┘  └────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┴───────────────┐
                ▼                               ▼
┌───────────────────────────┐   ┌───────────────────────────┐
│      OpenSearch           │   │       ArangoDB            │
│  (Searchable Assets)      │   │   (Graph Lineage)         │
│                           │   │                           │
│  • Processors (DATASET)   │   │  • Processor vertices     │
│  • Columns (COLUMN)       │   │  • Column vertices        │
│  • Tables (TABLE)         │   │  • Table vertices         │
│  • Databases (DATABASE)   │   │  • Processor edges        │
│  • Schemas (SCHEMA)       │   │  • Column lineage edges   │
└───────────────────────────┘   └───────────────────────────┘
                │                               │
                └───────────────┬───────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    React Frontend                                │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  UDFCatalogSearchV2.tsx (Hierarchy View)                 │   │
│  │    Database → Schema → Table → Columns                   │   │
│  │    Container → ProcessGroup → Processor → Columns        │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  UDFEntityPage.tsx (Entity Detail + Lineage Tab)         │   │
│  │    - Processor properties                                 │   │
│  │    - Column-level lineage graph                           │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## 🧪 Testing Plan

### 1. Build & Deploy
```bash
# Build C# API
docker-compose -f docker-compose.yml build csharp-api

# Start services
docker-compose up -d
```

### 2. Create Test NiFi Flow
1. Create NiFi container from UI
2. In NiFi, create flow:
   - ExecuteSQL processor (SELECT product_id, product_name FROM PRODUCTS.dbo.PRODUCTS)
   - ConvertAvroToJSON processor
   - PutDatabaseRecord processor (INSERT INTO PRODUCTS.CATALOGS.PRODUCTS)
3. Connect processors

### 3. Verify Ingestion
```bash
# Check logs
docker logs cudf-csharp-api

# Expected log entries:
# - "Calling GSP API for vendor: dbvmssql"
# - "GSP parsing complete: X tables, Y result columns, Z lineages"
# - "Indexed column nifi://container/.../column/product_id"
# - "Added column vertex: nifi://container/.../column/product_id"
# - "Adding column lineage edge from ... to ..."
```

### 4. Verify OpenSearch
```bash
curl http://localhost:9200/nifi-metadata/_search?q=type:COLUMN
curl http://localhost:9200/nifi-metadata/_search?q=type:TABLE
```

### 5. Verify ArangoDB
```bash
# Access ArangoDB UI: http://localhost:8529
# Database: nifi_metadata
# Collections: columns, tables, column_lineage
# Query: FOR v IN columns RETURN v
```

### 6. Verify Frontend
1. Navigate to: `http://localhost:5173/udf-catalog/search?platform=NiFi`
2. Expand hierarchy: nifi-flow → NiFi Flow → MyPG → Processors
3. Click processor → See "Lineage" tab
4. Verify column-level lineage graph displays

## 📝 Configuration

### GSP API Configuration
Add to `appsettings.json`:
```json
{
  "GSP": {
    "Url": "http://gsp.onprem.qa.octopai-corp.local/parser/gsp/dataflow",
    "Timeout": 30
  }
}
```

## 🔍 Troubleshooting

### GSP API Not Accessible
- Verify network connectivity: `curl http://gsp.onprem.qa.octopai-corp.local/parser/gsp/dataflow`
- Check firewall rules
- Verify GSP service is running

### No Columns Extracted
- Check processor properties contain schema information
- Verify SQL query is valid
- Check GSP API response in logs

### Column Lineage Not Showing
- Verify columns were indexed in OpenSearch
- Check ArangoDB has column vertices and edges
- Verify frontend is querying correct endpoint

## 🚀 Next Steps

1. Complete `NiFiMetadataIngestionService` integration (see Pending Integration section above)
2. Build and test
3. Verify processor-level lineage still works
4. Test column-level lineage end-to-end
5. Update frontend to display column lineage in Lineage tab
