# Database and Schema Extraction from NiFi Processors

## Overview

The system now extracts database and schema information from NiFi processor properties to create accurate table URNs for lineage tracking.

## PutDatabaseRecord Properties

The system extracts the following properties from PutDatabaseRecord processors:

### Property Mapping:
- **`Catalog Name`** → Database name
- **`Schema Name`** → Schema name  
- **`Table Name`** → Table name
- **`Database Connection Pooling Service`** → DBCP Service reference

### Example Configuration:
```
Catalog Name: SalesDB
Schema Name: dbo
Table Name: Orders
Database Connection Pooling Service: DBCPConnectionPool
```

### Resulting URN:
```
jdbc://localhost/SalesDB/dbo/Orders/column/PRODUCT_ID
```

## ExecuteSQL Properties

The system extracts:
- **`SQL select query`** → Parsed by GSP to extract source tables
- **`Database Connection Pooling Service`** → DBCP Service reference

### GSP Parsing:
The SQL query is parsed to extract:
- Source table names (e.g., `PRODUCTS.dbo.PRODUCTS`)
- Column names
- Column-to-column lineage

### Example:
```sql
SELECT PRODUCT_ID, PRODUCT_SKU, PRODUCT_NAME 
FROM PRODUCTS.dbo.PRODUCTS
```

Extracts:
- Database: `PRODUCTS`
- Schema: `dbo`
- Table: `PRODUCTS`
- Columns: `PRODUCT_ID`, `PRODUCT_SKU`, `PRODUCT_NAME`

## Complete Lineage Chain

### Source Table (from ExecuteSQL SQL):
```
jdbc://localhost/PRODUCTS/dbo/PRODUCTS/column/PRODUCT_ID
```

### ExecuteSQL Processor:
```
nifi://container/{id}/processor/{executeSQL-id}/column/PRODUCT_ID
```

### ConvertAvroToJSON Processor (propagated):
```
nifi://container/{id}/processor/{avro-id}/column/PRODUCT_ID
```

### PutDatabaseRecord Processor (propagated):
```
nifi://container/{id}/processor/{putDB-id}/column/PRODUCT_ID
```

### Target Table (from PutDatabaseRecord properties):
```
jdbc://localhost/SalesDB/dbo/Orders/column/PRODUCT_ID
```

## Implementation Details

### 1. PutDatabaseRecord Handling

```csharp
var tableName = properties.GetValueOrDefault("Table Name");
var catalogName = properties.GetValueOrDefault("Catalog Name"); // Database
var schemaName = properties.GetValueOrDefault("Schema Name");   // Schema
var dbcpService = properties.GetValueOrDefault("Database Connection Pooling Service");
```

### 2. Target Table Column Creation

```csharp
var database = catalogName ?? "unknown";
var schema = schemaName ?? "dbo";
var table = tableName;

var columnFqn = DatabaseFqn.CreateColumn(server, database, schema, table, columnName);
```

### 3. ExecuteSQL Handling

```csharp
var sqlQuery = properties.GetValueOrDefault("SQL select query");
var dbcpService = properties.GetValueOrDefault("Database Connection Pooling Service");

// Parse SQL with GSP
var gspResult = await _gspParser.ParseSqlAsync(sqlQuery, vendor, cancellationToken);

// GSP returns database, schema, table from SQL
foreach (var table in gspResult.SourceTables)
{
    // table.Database, table.Schema, table.Name
}
```

## Fallback Behavior

### If Properties Are Missing:

1. **Catalog Name (Database)**: Defaults to `"unknown"`
2. **Schema Name**: Defaults to `"dbo"`
3. **Table Name**: Must be provided (required)

### If GSP Parsing Fails:

The fallback SQL parser extracts:
- Table name from SQL (e.g., `PRODUCTS.dbo.PRODUCTS`)
- Splits into: database=`PRODUCTS`, schema=`dbo`, table=`PRODUCTS`

## Configuration Example

### NiFi Flow Setup:

1. **Create DBCP Service**:
   - Name: `ProductionDB`
   - Connection URL: `jdbc:sqlserver://localhost:1433;databaseName=PRODUCTS`
   - Driver: `Microsoft SQL Server`

2. **Configure ExecuteSQL**:
   - Database Connection Pooling Service: `ProductionDB`
   - SQL select query: `SELECT * FROM PRODUCTS.dbo.PRODUCTS`

3. **Configure PutDatabaseRecord**:
   - Database Connection Pooling Service: `ProductionDB`
   - Catalog Name: `SalesDB`
   - Schema Name: `dbo`
   - Table Name: `Orders`

## Verification

### Check Source Table URN:
```aql
FOR doc IN columns
  FILTER doc.urn LIKE "jdbc://localhost/PRODUCTS/dbo/PRODUCTS/column/%"
  RETURN doc.urn
```

### Check Target Table URN:
```aql
FOR doc IN columns
  FILTER doc.urn LIKE "jdbc://localhost/SalesDB/dbo/Orders/column/%"
  RETURN doc.urn
```

### Verify Complete Lineage:
```aql
FOR v, e, p IN 1..10 OUTBOUND 
  'columns/jdbc_localhost_PRODUCTS_dbo_PRODUCTS_column_PRODUCT_ID' 
  GRAPH 'column_lineage_graph'
  RETURN p.vertices[*].urn
```

Expected output:
```
[
  "jdbc://localhost/PRODUCTS/dbo/PRODUCTS/column/PRODUCT_ID",
  "nifi://container/.../processor/{executeSQL}/column/PRODUCT_ID",
  "nifi://container/.../processor/{avro}/column/PRODUCT_ID",
  "nifi://container/.../processor/{putDB}/column/PRODUCT_ID",
  "jdbc://localhost/SalesDB/dbo/Orders/column/PRODUCT_ID"
]
```

## Benefits

✅ **Accurate URNs**: Uses actual database and schema names from configuration

✅ **Cross-Database Lineage**: Can track data movement between different databases

✅ **Schema-Aware**: Distinguishes between tables in different schemas

✅ **DBCP Integration**: References the same connection pool service used by processors

✅ **Flexible**: Falls back to defaults if properties are missing

## Future Enhancements

### DBCP Service Parsing:
Currently, the DBCP Service reference is stored but not parsed. Future enhancement could:
1. Fetch DBCP Service configuration from NiFi
2. Parse JDBC connection URL
3. Extract server, port, database from URL
4. Use for more accurate URNs

### Example DBCP URL Parsing:
```
jdbc:sqlserver://prod-server:1433;databaseName=PRODUCTS
```
Extracts:
- Server: `prod-server`
- Port: `1433`
- Database: `PRODUCTS`
