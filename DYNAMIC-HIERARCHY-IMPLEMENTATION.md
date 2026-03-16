# Dynamic NiFi Hierarchy Implementation

## Overview
Implemented a **flexible, dynamic hierarchy system** that supports arbitrary nesting depth of NiFi process groups, replacing the previous hardcoded 2-level approach.

## Key Changes

### 1. Ancestor Chain Storage
Instead of storing only parent and grandparent, we now store the **complete ancestor chain** from root to the processor's immediate parent.

#### Data Structure
```json
{
  "ancestorChain": [
    {"id": "root-pg-id", "name": "NiFi Flow"},
    {"id": "child-pg-id", "name": "MyPG"},
    {"id": "grandchild-pg-id", "name": "SubPG"}
    // ... any number of levels
  ]
}
```

### 2. Ingestion Service Changes

#### Method Signatures
**Before:**
```csharp
IngestProcessGroupAsync(
    HttpClient httpClient,
    string containerId,
    string processGroupId,
    string? parentProcessGroupId,
    string? parentProcessGroupName,
    CancellationToken cancellationToken)

IngestProcessorAsync(
    JsonElement processorElement,
    string containerId,
    string parentProcessGroupId,
    string? parentProcessGroupName,
    string? grandparentProcessGroupId,
    string? grandparentProcessGroupName,
    CancellationToken cancellationToken)
```

**After:**
```csharp
IngestProcessGroupAsync(
    HttpClient httpClient,
    string containerId,
    string processGroupId,
    List<(string id, string name)> parentChain,
    CancellationToken cancellationToken)

IngestProcessorAsync(
    JsonElement processorElement,
    string containerId,
    string parentProcessGroupId,
    List<(string id, string name)> ancestorChain,
    CancellationToken cancellationToken)
```

#### Recursive Chain Building
```csharp
// In IngestProcessGroupAsync:
// Build the full ancestor chain for processors in this group
var currentAncestorChain = new List<(string id, string name)>(parentChain);
if (!string.IsNullOrWhiteSpace(processGroupName))
{
    currentAncestorChain.Add((processGroupId, processGroupName));
}

// Pass to child process groups
count += await IngestProcessGroupAsync(httpClient, containerId, childPgId, currentAncestorChain, cancellationToken);
```

#### JSON Serialization
```csharp
// Convert tuples to anonymous objects for proper JSON serialization
var ancestorObjects = ancestorChain.Select(a => new { id = a.id, name = a.name }).ToList();
var ancestorJson = System.Text.Json.JsonSerializer.Serialize(ancestorObjects);
properties["ancestorChain"] = ancestorJson;
```

### 3. Search Handler Changes

#### Dynamic Hierarchy Building
**Before:** Hardcoded to handle only parent + grandparent
**After:** Dynamically parses the ancestor chain to build the full hierarchy

```csharp
// Parse the ancestor chain from processor properties
if (processor.Properties != null && processor.Properties.TryGetValue("ancestorChain", out var ancestorChainJson))
{
    var ancestorChain = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(ancestorChainJson);
    
    if (ancestorChain != null && ancestorChain.Count > 0)
    {
        // Build process group entities for each ancestor in the chain
        string? previousPgUrn = null;
        
        for (int i = 0; i < ancestorChain.Count; i++)
        {
            var ancestor = ancestorChain[i];
            var pgId = ancestor["id"];
            var pgName = ancestor["name"];
            var pgUrn = $"nifi://container/{containerId}/process-group/{pgId}";
            
            // First ancestor's parent is container, others point to previous ancestor
            var parentUrn = i == 0 ? null : previousPgUrn;
            processGroupInfo[pgUrn] = (containerId, pgId, pgName, parentUrn);
            
            previousPgUrn = pgUrn;
        }
    }
}
```

## Example Hierarchies

### 2-Level Hierarchy (Current)
```
nifi-flow (Container)
  └── NiFi Flow (Root PG)
      └── MyPG (Child PG)
          ├── Processor1
          ├── Processor2
          └── Processor3
```

**Ancestor Chain for Processor1:**
```json
[
  {"id":"d1c723b3-019c-1000-0379-f5e3cdf40deb","name":"NiFi Flow"},
  {"id":"d6a7ded8-019c-1000-0f77-676467fb109f","name":"MyPG"}
]
```

### 4-Level Hierarchy (Supported)
```
nifi-flow (Container)
  └── NiFi Flow (Root PG)
      └── DataIngestion (Level 1 PG)
          └── Kafka (Level 2 PG)
              └── Transformations (Level 3 PG)
                  ├── Processor1
                  └── Processor2
```

**Ancestor Chain for Processor1:**
```json
[
  {"id":"root-id","name":"NiFi Flow"},
  {"id":"level1-id","name":"DataIngestion"},
  {"id":"level2-id","name":"Kafka"},
  {"id":"level3-id","name":"Transformations"}
]
```

## Benefits

### 1. **Unlimited Nesting**
- Supports any depth of process group nesting
- No hardcoded limits
- Automatically adapts to NiFi's structure

### 2. **Maintainability**
- Single data structure (ancestor chain) instead of multiple parent/grandparent fields
- Recursive algorithm scales naturally
- Easy to understand and debug

### 3. **Performance**
- Ancestor chain built once during ingestion
- Search handler efficiently reconstructs hierarchy
- No additional NiFi API calls needed

### 4. **Extensibility**
- Easy to add metadata to ancestor chain (e.g., timestamps, descriptions)
- Can support other hierarchical systems beyond NiFi
- Foundation for advanced features (breadcrumbs, path-based search)

## Testing

### Verify Ancestor Chain Storage
```powershell
curl "http://localhost:5000/api/atlas/search?query=*&platform=NiFi&count=50" | ConvertFrom-Json | Select-Object -ExpandProperty results | Where-Object { $_.type -eq 'DATASET' } | ForEach-Object { 
    Write-Host "Processor: $($_.name)"
    Write-Host "  Ancestor Chain: $($_.properties.ancestorChain)"
}
```

### Verify Hierarchy Display
1. Navigate to `http://localhost:5173/udf-catalog/search?platform=NiFi`
2. Verify all levels of the hierarchy are displayed
3. Expand each level to verify correct parent-child relationships
4. Click on processors to verify navigation works

## Files Modified

### Backend
1. **`NiFiMetadataIngestionService.cs`**
   - Changed `IngestProcessGroupAsync` to accept `List<(string id, string name)> parentChain`
   - Changed `IngestProcessorAsync` to accept `List<(string id, string name)> ancestorChain`
   - Added ancestor chain building logic
   - Fixed JSON serialization using anonymous objects

2. **`SearchEntitiesQueryHandler.cs`**
   - Updated `BuildHierarchyWithContainers` to parse ancestor chain JSON
   - Removed hardcoded grandparent logic
   - Added dynamic loop to build all ancestor entities

### Frontend
No changes required - the existing hierarchy rendering logic automatically handles any depth!

## Future Enhancements

1. **Breadcrumb Navigation**: Use ancestor chain to show full path in UI
2. **Path-based Search**: Search by hierarchy path (e.g., "NiFi Flow > MyPG > *")
3. **Hierarchy Visualization**: Tree diagram showing full structure
4. **Performance Optimization**: Cache ancestor chains for faster lookups
5. **Cross-platform Support**: Extend to other hierarchical systems (Airflow DAGs, dbt models, etc.)
