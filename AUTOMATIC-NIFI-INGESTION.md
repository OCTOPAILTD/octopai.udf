# Automatic NiFi Metadata Ingestion

## Overview

The system now automatically ingests metadata from running NiFi containers into both OpenSearch (for search) and ArangoDB (for lineage).

## How It Works

### 1. Background Monitor Service

**Service:** `NiFiMetadataMonitorService` (Background Service)
- **Polling Interval:** Every 30 seconds
- **What it does:**
  - Scans all running Docker containers
  - Identifies NiFi containers by name or image
  - Triggers metadata ingestion for each NiFi container
  - Tracks last ingestion time (re-ingests every 5 minutes minimum)

### 2. Metadata Ingestion Service

**Service:** `NiFiMetadataIngestionService`
- **Connects to:** NiFi REST API at `http://{container-name}:8080/nifi-api`
- **Extracts:**
  - All processors from root process group
  - All processors from nested process groups (recursive)
  - All connections between processors (for lineage)

### 3. Data Storage

**OpenSearch** (`nifi-processors` index):
- Stores processor metadata for search
- Fields: name, type, description, properties, status, platform
- Enables full-text search and filtering

**ArangoDB** (Graph database):
- Stores processor vertices
- Stores lineage edges (connections between processors)
- Enables lineage traversal and visualization

## What Gets Ingested

From each NiFi container, the system extracts:

1. **Processors:**
   - Processor ID and Name
   - Processor Type (e.g., `org.apache.nifi.processors.standard.ExecuteSQL`)
   - Status (Running/Stopped/Disabled)
   - Configuration properties
   - Description/comments
   - Parent process group

2. **Connections (Lineage):**
   - Source processor → Destination processor
   - Relationship type: `Lineage`
   - Stored as edges in ArangoDB graph

## API Endpoints

### Trigger Manual Ingestion (Optional)
```
POST /api/atlas/ingest/nifi/{containerId}
```

Response:
```json
{
  "success": true,
  "entitiesIngested": 3,
  "message": "Successfully ingested 3 entities from NiFi container abc123"
}
```

### Search Ingested Data
```
GET /api/atlas/search?query=*&platform=NiFi&count=50
```

## Fixed Issues

### 1. ✅ Deleted Sample Data
- Removed all hardcoded sample data from OpenSearch
- Now only shows real data from running NiFi containers

### 2. ✅ Fixed "Open NiFi" URL
- Changed from: `http://192.168.1.131:9090/nifi/`
- Changed to: `http://localhost:8080/nifi/`

### 3. ✅ Fixed Platform Filter
- OpenSearch query now correctly filters by `platform` field
- Changed from: `platform.keyword` (incorrect)
- Changed to: `platform` (correct - field is already keyword type)

### 4. ✅ Automatic Ingestion
- Background service monitors all NiFi containers
- Automatically ingests metadata every 30 seconds
- No manual trigger needed

## Testing

1. **Create a NiFi container** via the UI at `http://localhost:5173/workspace/w1`
2. **Wait 30 seconds** for automatic ingestion
3. **View data** at `http://localhost:5173/udf-catalog/search?platform=NiFi`
4. **See your real processors** from NiFi in the hierarchical tree view

## Logs

Monitor the ingestion process:
```bash
docker logs nifi-metadata-api --tail 50 -f
```

Look for:
- `[INF] NiFi Metadata Monitor Service started`
- `[INF] Ingesting metadata from NiFi container: {name}`
- `[INF] Successfully ingested {count} entities from {name}`

## Architecture

```
┌─────────────────┐
│  NiFi Container │
│   (port 8080)   │
└────────┬────────┘
         │ REST API
         │
┌────────▼────────────────────────┐
│  NiFiMetadataMonitorService     │
│  (Background Service)            │
│  - Polls every 30s               │
│  - Detects NiFi containers       │
└────────┬────────────────────────┘
         │
┌────────▼────────────────────────┐
│  NiFiMetadataIngestionService   │
│  - Calls NiFi API                │
│  - Extracts processors           │
│  - Extracts connections          │
└────────┬────────────────────────┘
         │
         ├──────────────┬──────────────┐
         │              │              │
┌────────▼──────┐  ┌───▼────────┐  ┌──▼──────────┐
│  OpenSearch   │  │  ArangoDB  │  │   Redis     │
│  (Search)     │  │  (Lineage) │  │  (Cache)    │
└───────────────┘  └────────────┘  └─────────────┘
```

## Next Steps

When you add/modify/delete processors in NiFi:
1. Changes are detected within 30 seconds
2. Metadata is automatically re-ingested
3. UI updates with latest data
4. Lineage graph is updated in ArangoDB

No manual intervention required! 🚀
