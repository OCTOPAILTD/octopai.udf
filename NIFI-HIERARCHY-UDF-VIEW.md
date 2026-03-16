# NiFi Hierarchy View - "Open in UDF" Feature

## Overview

The "Open in UDF" button now opens a **hierarchical view** of your NiFi container, showing:
- **Container** (top level)
- **Process Groups** (folders/nested flows)
- **Processors** (individual components)

Each processor has a **Lineage tab** that shows upstream and downstream data connections.

## What You'll See

### Hierarchical Tree Structure

```
📦 NiFi Container (nifi-demo)
  └─ 📁 Process Group: root
      ├─ ⚙️ GetFile - Read Customer Data
      ├─ ⚙️ ConvertRecord - Transform Customer Data
      ├─ ⚙️ QueryRecord - Filter Active Customers
      ├─ ⚙️ PublishKafka - Send Customer Events
      └─ ⚙️ PutFile - Write to Data Lake
```

### Two-Panel Layout

**Left Panel**: Hierarchical tree
- Expandable/collapsible nodes
- Click to select a processor
- "Lineage" button on each node

**Right Panel**: Details & Lineage
- Processor details (name, description, properties)
- Lineage visualization when you click "View Lineage"
- Shows upstream (inputs) and downstream (outputs)

## How to Use

### 1. Navigate to Workspace

```
http://localhost:5173/workspace/w1
```

### 2. Find Your NiFi Container

Look in the "Items" table for your NiFi container (e.g., "nifi-demo")

### 3. Click "Open in UDF" Button

The **green button** in the Actions column will now open the hierarchy view instead of the search page.

### 4. Explore the Hierarchy

- **Click the chevron** (▶/▼) to expand/collapse process groups
- **Click a processor** to see its details in the right panel
- **Click "View Lineage"** to see data flow connections

### 5. View Lineage

When you click "View Lineage" on a processor, you'll see:

**Upstream (Inputs)**:
```
┌─────────────────────────────────┐
│ 📊 GetFile - Read Customer Data │
│ Type: DATA_JOB                  │
└─────────────────────────────────┘
           ↓
```

**Current Processor**:
```
┌────────────────────────────────────────┐
│ ConvertRecord - Transform Customer Data │
└────────────────────────────────────────┘
           ↓
```

**Downstream (Outputs)**:
```
┌──────────────────────────────────────┐
│ 📊 QueryRecord - Filter Active Customers │
│ Type: DATA_JOB                       │
└──────────────────────────────────────┘
```

## Technical Implementation

### New Component: `NiFiHierarchyView.tsx`

**Features**:
- Hierarchical tree rendering with expand/collapse
- Two-panel layout (tree + details)
- Lineage visualization per processor
- Click-to-select navigation
- Auto-expand first level

**API Endpoints Used**:
- `GET /api/atlas/hierarchy/containers` - Get all processors
- `GET /api/atlas/lineage/{urn}` - Get lineage for specific processor

### Updated Components

#### `WorkspaceCanvas.tsx`
Changed "Open in UDF" button from:
```typescript
navigate('/udf-catalog/search?platform=NiFi');
```

To:
```typescript
navigate(`/nifi-hierarchy/${item.id}`);
```

#### `App.tsx`
Added new route:
```typescript
<Route path="/nifi-hierarchy/:containerId" element={<NiFiHierarchyView />} />
```

## Data Structure

### Hierarchy Node

```typescript
interface HierarchyNode {
  urn: string;                    // Unique identifier
  type: string;                   // CONTAINER, PROCESS_GROUP, DATA_JOB
  name: string;                   // Display name
  platform?: string;              // "NiFi"
  description?: string;           // Processor description
  properties?: Record<string, string>;  // Configuration properties
  parentContainerUrn?: string;    // Parent URN for tree building
  children?: HierarchyNode[];     // Child nodes
  isExpanded?: boolean;           // UI state
  level?: number;                 // Tree depth
}
```

### Lineage Data

```typescript
interface LineageData {
  nodes: Array<{
    urn: string;
    name: string;
    type: string;
    direction: 'upstream' | 'downstream';
  }>;
  edges: Array<{
    from: string;
    to: string;
  }>;
}
```

## Example Workflow

### Step 1: Create NiFi Container
```
Workspace w1 → "+ New item" → "NiFi Flow" → Container created
```

### Step 2: Open in UDF
```
Workspace w1 → Find "nifi-demo" row → Click "Open in UDF" (green button)
```

### Step 3: View Hierarchy
```
Left Panel:
  📦 nifi-demo
    └─ 📁 root
        ├─ ⚙️ GetFile - Read Customer Data
        ├─ ⚙️ ConvertRecord - Transform Customer Data
        └─ ⚙️ PublishKafka - Send Customer Events
```

### Step 4: Select Processor
```
Click "GetFile - Read Customer Data"

Right Panel shows:
  - Name: GetFile - Read Customer Data
  - Description: Reads customer CSV files...
  - Properties:
    • Input Directory: /data/input/customers
    • File Filter: customer_.*\.csv
    • Keep Source File: false
```

### Step 5: View Lineage
```
Click "View Lineage" button

Shows:
  Upstream: (none - this is the source)
  Current: GetFile - Read Customer Data
  Downstream: ConvertRecord - Transform Customer Data
```

## UI Features

### Tree Navigation
- **Expand/Collapse**: Click chevron icon (▶/▼)
- **Select Node**: Click anywhere on the row
- **Visual Feedback**: Selected node highlighted in blue
- **Indentation**: Shows hierarchy depth visually

### Details Panel
- **Auto-scroll**: Details panel scrolls independently
- **Properties Table**: Key-value pairs from processor config
- **URN Display**: Full URN in monospace font
- **Platform Badge**: Shows "NiFi" badge

### Lineage Panel
- **Color-coded**: Blue for upstream, green for downstream
- **Direction Labels**: "Upstream (Inputs)" and "Downstream (Outputs)"
- **Connection Count**: Shows number of edges
- **Empty State**: Friendly message if no lineage

## Current Status

✅ **Component Created**: `src/pages/NiFiHierarchyView.tsx`
✅ **Route Added**: `/nifi-hierarchy/:containerId`
✅ **Button Updated**: "Open in UDF" now navigates to hierarchy view
✅ **Frontend Restarted**: Changes applied
✅ **Test Container**: `nifi-demo` created and ready

## Testing

### 1. View Existing Data
```
http://localhost:5173/workspace/w1
```

Click "Open in UDF" on the `nifi-demo` container.

### 2. Expected Result

You should see:
- Left panel with hierarchical tree of processors
- Right panel with "Select a processor to view details" message
- Click any processor to see its details
- Click "View Lineage" to see data flow

### 3. Verify Lineage

Select any processor and click "View Lineage". You should see:
- Upstream processors (what feeds into this processor)
- Downstream processors (what this processor feeds into)
- Visual representation with colored boxes

## API Endpoints

### Get Hierarchy
```
GET /api/atlas/hierarchy/containers
```

Returns all containers with their processors and parent relationships.

### Get Lineage
```
GET /api/atlas/lineage/{urn}?direction=BOTH&depth=3&includeColumns=false
```

Returns lineage graph for a specific processor.

**Parameters**:
- `urn`: Processor URN (e.g., `nifi://container/nifi-demo/processor/getfile-001`)
- `direction`: `UPSTREAM`, `DOWNSTREAM`, or `BOTH`
- `depth`: How many hops to traverse (default: 3)
- `includeColumns`: Include column-level lineage (default: false)

## Sample Data

The system currently has sample data with 5 processors:

1. **GetFile - Read Customer Data**
   - Reads CSV files from `/data/input/customers`
   - Source processor (no upstream)

2. **ConvertRecord - Transform Customer Data**
   - Converts CSV to JSON
   - Upstream: GetFile

3. **QueryRecord - Filter Active Customers**
   - Filters using SQL: `SELECT * FROM FLOWFILE WHERE status = 'active'`
   - Upstream: ConvertRecord

4. **PublishKafka - Send Customer Events**
   - Publishes to `customer-updates` topic
   - Upstream: QueryRecord

5. **PutFile - Write to Data Lake**
   - Writes to `/data/lake/customers`
   - Upstream: QueryRecord

## Troubleshooting

### "No NiFi containers found"

**Cause**: No data in OpenSearch index

**Solution**:
1. Check if sample data exists:
   ```powershell
   curl "http://localhost:9200/nifi-processors/_search?size=1"
   ```

2. If empty, add sample data (see previous documentation)

### "Failed to load hierarchy"

**Cause**: API connection issue

**Solution**:
1. Verify API is running:
   ```powershell
   curl http://localhost:5000/api/atlas/hierarchy/containers
   ```

2. Check API logs:
   ```powershell
   docker logs nifi-metadata-api --tail 50
   ```

### Lineage shows "No lineage data available"

**Cause**: Processor has no connections in the data

**Solution**: This is expected for:
- Source processors (no upstream)
- Sink processors (no downstream)
- Isolated processors

## Next Steps

1. **Create a NiFi container** (if you removed all):
   ```
   Workspace w1 → "+ New item" → "NiFi Flow"
   ```

2. **Click "Open in UDF"** on the container row

3. **Explore the hierarchy**:
   - Expand process groups
   - Click processors to see details
   - View lineage for each processor

4. **Test lineage visualization**:
   - Select "ConvertRecord" processor
   - Click "View Lineage"
   - See GetFile (upstream) and QueryRecord (downstream)

## Visual Guide

### Workspace View
```
┌─────────────────────────────────────────────────────────────┐
│ DataEngineering1                                            │
├─────────────────────────────────────────────────────────────┤
│ Name              Status    Type      Container ID  Actions │
│ ⚙️ nifi-demo      ✅ Ready  NiFi Flow  7695a85ba3   [Open NiFi] [Open In Octopai] [Open in UDF] │
└─────────────────────────────────────────────────────────────┘
                                                          ↑
                                                    Click this!
```

### Hierarchy View
```
┌─────────────────────────────────┬─────────────────────────────────┐
│ Process Groups & Processors     │ Details & Lineage               │
├─────────────────────────────────┼─────────────────────────────────┤
│                                 │                                 │
│ ▼ 📦 nifi-demo                  │ GetFile - Read Customer Data    │
│   └─ ▼ 📁 root                  │                                 │
│       ├─ ⚙️ GetFile             │ Description:                    │
│       ├─ ⚙️ ConvertRecord  ←─── │ Reads customer CSV files...     │
│       ├─ ⚙️ QueryRecord         │                                 │
│       ├─ ⚙️ PublishKafka        │ Properties:                     │
│       └─ ⚙️ PutFile             │ • Input Directory: /data/input  │
│                                 │ • File Filter: customer_.*\.csv │
│                                 │                                 │
│                                 │ [View Lineage]                  │
│                                 │                                 │
│                                 │ ─── Lineage ───                 │
│                                 │                                 │
│                                 │ Downstream (Outputs):           │
│                                 │ ┌─────────────────────────┐    │
│                                 │ │ ConvertRecord           │    │
│                                 │ │ Type: DATA_JOB          │    │
│                                 │ └─────────────────────────┘    │
└─────────────────────────────────┴─────────────────────────────────┘
```

## Summary

✅ **"Open in UDF" button** now shows hierarchical view
✅ **ProcessorGroups** displayed as expandable folders
✅ **Processors** shown under their parent groups
✅ **Lineage tab** available for each processor
✅ **Two-panel layout** for easy navigation
✅ **Real-time updates** via API

**Ready to use!** Navigate to `http://localhost:5173/workspace/w1` and click "Open in UDF" on any NiFi container.
