# Verify ArangoDB Edges

## Current Status

Based on the API logs, the system IS creating edges:

```
[09:43:29 INF] Adding edge from nifi://container/.../processor/1f201ded... 
                to nifi://container/.../processor/e0a94826..., type: Lineage

[09:43:29 INF] Adding edge from nifi://container/.../processor/e0a94826... 
                to nifi://container/.../processor/3fed7da4..., type: Lineage
```

## Where to Look for Edges

### 1. Access ArangoDB Web UI
- URL: `http://localhost:8529`
- Username: `root`
- Password: (empty)

### 2. Select Database
- Click "Databases" in the left menu
- Select `nifi_metadata` database

### 3. Check Collections

**For Processor-Level Lineage:**
- Collection: `lineage` (Edge collection)
- Query:
```aql
FOR edge IN lineage
  RETURN edge
```

**For Column-Level Lineage:**
- Collection: `column_lineage` (Edge collection)
- Query:
```aql
FOR edge IN column_lineage
  RETURN edge
```

### 4. Check Vertices

**Processors:**
```aql
FOR v IN processors
  RETURN {
    key: v._key,
    fqn: v.fqn,
    name: v.name
  }
```

**Columns:**
```aql
FOR v IN columns
  RETURN {
    key: v._key,
    fqn: v.fqn
  }
```

**Tables:**
```aql
FOR v IN tables
  RETURN {
    key: v._key,
    fqn: v.fqn
  }
```

## Expected Results

Based on the logs, you should see:

### Processor Vertices (3)
1. `nifi_container_9e57bd20..._processor_e0a94826...`
2. `nifi_container_9e57bd20..._processor_3fed7da4...`
3. `nifi_container_9e57bd20..._processor_1f201ded...`

### Processor Edges (2)
1. `1f201ded... → e0a94826...`
2. `e0a94826... → 3fed7da4...`

### Column Edges
- Currently 0 (because GSP parsing failed)
- Error: "Data at the root level is invalid"
- This means GSP API returned invalid XML or empty response

## Troubleshooting

### If you don't see edges in `lineage` collection:

1. **Check if you're in the right database:**
   - Make sure you selected `nifi_metadata` (not `_system`)

2. **Check collection type:**
   - `lineage` should be an Edge collection (not Document)
   - Look for the edge icon in the collections list

3. **Verify graph exists:**
   - Go to "Graphs" in left menu
   - Should see `nifi_lineage_graph`
   - Click it to visualize

4. **Check for old data:**
   - If you had previous ingestion runs, old edges might be in a different collection
   - The new code creates edges in `lineage` collection

### If GSP is failing:

The error "Data at the root level is invalid" means:
- GSP API is not accessible
- GSP API returned empty response
- GSP API returned non-XML response

**To fix:**
1. Verify GSP service is accessible
2. Check if SQL query is valid
3. Add better error handling in GSPSqlParserService

## Quick Test

Run this in ArangoDB Web UI (Query tab):

```aql
// Count all edges
RETURN {
  processor_edges: LENGTH(lineage),
  column_edges: LENGTH(column_lineage)
}
```

This will tell you exactly how many edges exist in each collection.
