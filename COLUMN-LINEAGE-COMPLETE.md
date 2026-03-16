# Complete Column-Level Lineage Implementation

## Overview

The system now tracks complete end-to-end column-level lineage from source tables through NiFi processors to target tables.

## Lineage Flow

For your NiFi flow with SQL query:
```sql
SELECT PRODUCT_ID, PRODUCT_SKU, PRODUCT_NAME, CATEGORY_ID, CREATED_AT
FROM PRODUCTS.dbo.PRODUCTS
```

The complete lineage chain is:

```
Source Table Column → ExecuteSQL Column → ConvertAvroToJSON Column → PutDatabaseRecord Column → Target Table Column
```

### Example for PRODUCT_ID:

1. **Source**: `jdbc://localhost/unknown/dbo/PRODUCTS/column/PRODUCT_ID`
2. **ExecuteSQL**: `nifi://container/{id}/processor/{executeSQL-id}/column/PRODUCT_ID`
3. **ConvertAvroToJSON**: `nifi://container/{id}/processor/{avro-id}/column/PRODUCT_ID`
4. **PutDatabaseRecord**: `nifi://container/{id}/processor/{putDB-id}/column/PRODUCT_ID`
5. **Target**: `jdbc://localhost/unknown/dbo/{target-table}/column/PRODUCT_ID`

## Total Entities in ArangoDB

### Columns (25 total):
- **5** from source table (PRODUCTS.dbo.PRODUCTS)
- **5** from ExecuteSQL processor
- **5** from ConvertAvroToJSON processor (propagated)
- **5** from PutDatabaseRecord processor (propagated)
- **5** from target table

### Edges (20 total):
- **5** from source table → ExecuteSQL (extracted from GSP lineage)
- **5** from ExecuteSQL → ConvertAvroToJSON (connection-based)
- **5** from ConvertAvroToJSON → PutDatabaseRecord (connection-based)
- **5** from PutDatabaseRecord → target table (propagation-based)

## Implementation Details

### 1. Source Table → ExecuteSQL Edges

Created from GSP (Gudu SQLFlow) API response:
- GSP parses the SQL query
- Identifies source tables and columns
- Returns column-level lineage relationships
- System creates edges from table columns to processor columns

**Code**: `CreateTableToProcessorEdgesAsync()` in `NiFiMetadataIngestionService.cs`

### 2. ExecuteSQL → ConvertAvroToJSON Edges

Created from NiFi connection metadata:
- System detects connection between processors
- Propagates columns from source to destination
- Creates column-to-column edges

**Code**: `IngestColumnLineageAsync()` with column propagation

### 3. ConvertAvroToJSON → PutDatabaseRecord Edges

Same as above - connection-based propagation.

### 4. PutDatabaseRecord → Target Table Edges

Created during target table column creation:
- System detects PutDatabaseRecord has a "Table Name" property
- Creates target table columns
- Creates edges from processor columns to table columns

**Code**: `CreateTargetTableColumnsAndEdgesAsync()` in `NiFiMetadataIngestionService.cs`

## Querying Lineage

### Get all downstream columns for PRODUCT_ID:

```aql
FOR v, e, p IN 1..10 OUTBOUND 
  'columns/jdbc_localhost_unknown_dbo_PRODUCTS_column_PRODUCT_ID' 
  GRAPH 'column_lineage_graph'
  RETURN {
    vertex: v.urn,
    path: p.vertices[*].urn
  }
```

### Get all upstream columns for a target column:

```aql
FOR v, e, p IN 1..10 INBOUND 
  'columns/jdbc_localhost_unknown_dbo_target_table_column_PRODUCT_ID' 
  GRAPH 'column_lineage_graph'
  RETURN {
    vertex: v.urn,
    path: p.vertices[*].urn
  }
```

### Get complete lineage path:

```aql
FOR v, e, p IN 1..10 ANY 
  'columns/nifi_container_{id}_processor_{executeSQL-id}_column_PRODUCT_ID' 
  GRAPH 'column_lineage_graph'
  RETURN p
```

## Verification Steps

### 1. Check Total Columns (should be 25):

```aql
FOR doc IN columns
  RETURN doc
```

Or via PowerShell:
```powershell
Invoke-WebRequest -Uri "http://localhost:8529/_db/nifi_metadata/_api/cursor" `
  -Method POST `
  -Body '{"query":"FOR doc IN columns RETURN doc"}' `
  -ContentType "application/json" | 
  Select-Object -ExpandProperty Content | 
  ConvertFrom-Json | 
  Select-Object -ExpandProperty result | 
  Measure-Object | 
  Select-Object -ExpandProperty Count
```

### 2. Check Total Edges (should be 20):

```aql
FOR edge IN column_lineage
  RETURN edge
```

Or via PowerShell:
```powershell
Invoke-WebRequest -Uri "http://localhost:8529/_db/nifi_metadata/_api/cursor" `
  -Method POST `
  -Body '{"query":"FOR edge IN column_lineage RETURN edge"}' `
  -ContentType "application/json" | 
  Select-Object -ExpandProperty Content | 
  ConvertFrom-Json | 
  Select-Object -ExpandProperty result | 
  Measure-Object | 
  Select-Object -ExpandProperty Count
```

### 3. Visualize Lineage in ArangoDB UI:

1. Go to http://localhost:8529
2. Select database: `nifi_metadata`
3. Go to "GRAPHS" tab
4. Select graph: `column_lineage_graph`
5. Click on any column vertex to see its connections

## Key Features

✅ **Complete End-to-End Lineage**: From source database tables through all NiFi processors to target tables

✅ **Column-Level Granularity**: Tracks individual column transformations, not just table/processor level

✅ **Automatic Propagation**: Columns automatically flow through processors that don't explicitly define schemas

✅ **Idempotent**: Re-running ingestion doesn't create duplicate edges

✅ **Bidirectional**: Can trace both upstream (where did this come from?) and downstream (where does this go?)

✅ **Graph Traversal**: Use ArangoDB's powerful graph queries to analyze lineage

## Architecture

### ArangoDB (Column Lineage Only):
- **Collections**: 
  - `columns` (vertices): Stores column URNs only
  - `column_lineage` (edges): Stores column-to-column relationships
- **Graph**: `column_lineage_graph`
- **Purpose**: Fast graph traversal for lineage queries

### OpenSearch (All Other Metadata):
- **Index**: `nifi_metadata`
- **Stores**:
  - Full column metadata (name, type, description, etc.)
  - Processor metadata
  - Table metadata
  - Hierarchical relationships
- **Purpose**: Search, filtering, and detailed metadata retrieval

## Why This Architecture?

1. **Separation of Concerns**: Graph database for graph queries, search engine for search queries
2. **Performance**: ArangoDB optimized for graph traversal, OpenSearch for full-text search
3. **Scalability**: Each system handles what it does best
4. **Flexibility**: Can add more metadata to OpenSearch without affecting lineage performance
5. **Simplicity**: Column lineage in ArangoDB is just URNs and edges - minimal data model

## Troubleshooting

### No edges showing up?

1. Check if processors are connected in NiFi
2. Check if ExecuteSQL has a SQL query configured
3. Check if PutDatabaseRecord has a "Table Name" configured
4. Check API logs for "Creating table-to-processor edge" messages

### Missing source/target table columns?

1. Verify GSP API is accessible (check logs for "GSP API response")
2. If GSP fails, fallback parser should work for simple SELECT statements
3. Check that table name in SQL matches expected format

### Duplicate edges?

The system uses `EdgeExistsAsync()` to check before creating edges. If you see duplicates:
1. Check ArangoDB logs
2. Verify idempotency logic in `ArangoDbRepository.AddColumnLineageEdgeAsync()`
