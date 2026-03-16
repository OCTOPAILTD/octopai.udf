# Creating NiFi Containers in Workspace

## Overview
This guide shows you how to create new NiFi 1.12 containers through the UI and see them appear in your workspace with the "Open in UDF" button.

## Current Status
✅ **Backend API**: Running on `http://localhost:5000`
✅ **Frontend UI**: Running on `http://localhost:5173`
✅ **Existing Containers**: 
   - `nifi-flow` (workspace: w1, ports: 8080/8443)
   - `nifi-test-container` (workspace: w1, ports: 8081/8444)

## Step-by-Step Guide

### 1. Navigate to Workspace
Open your browser and go to:
```
http://localhost:5173/workspace/w1
```

### 2. Create New NiFi Container

1. **Click the "+ New item" button** in the toolbar (top left)
2. **Select "NiFi Flow"** from the panel that appears
   - Icon: Workflow icon
   - Description: "Create data ingestion and ETL pipelines with Apache NiFi"
   - Tool: Apache NiFi

3. **Watch the Progress Bar**
   - A draggable modal window will appear showing:
     - Progress percentage (0-100%)
     - Current step (e.g., "Pulling image...", "Starting container...", "Waiting for NiFi to be ready...")
     - Real-time logs streaming from the container
   - The progress bar is implemented in `src/components/ContainerCreationProgress.tsx`
   - It uses Server-Sent Events (SSE) to stream progress from the backend

4. **Container Creation Process**
   - **Step 1**: API creates Docker container with NiFi 1.12.1 image
   - **Step 2**: Container starts and begins initialization
   - **Step 3**: Health checks monitor NiFi startup (polls `/nifi-api/system-diagnostics`)
   - **Step 4**: When ready, you'll see "Container is ready!" message
   - **Step 5**: Modal automatically closes and navigates to the NiFi embed page

### 3. View Container in Workspace

After creation, the container will appear in the workspace table with:

| Column | Description |
|--------|-------------|
| **Name** | Container name (e.g., "nifi-flow") with workflow icon |
| **Status** | Shows "Starting..." (yellow) or "Ready" (green) with health indicator |
| **Type** | "NiFi Flow" |
| **Container ID** | First 12 characters of Docker container ID |
| **Actions** | Three buttons (see below) |

### 4. Available Actions

Each NiFi container row has three action buttons:

1. **Open NiFi** (Blue link with external icon)
   - Opens native NiFi UI in new tab
   - URL: `http://192.168.1.131:9090/nifi/`
   - Direct access to NiFi canvas

2. **Open In Octopai** (Blue button with graph icon)
   - Opens DataHub lineage view for this container
   - Shows process groups and processors
   - URL: `http://192.168.1.131:9002/container/...`

3. **Open in UDF** (Green button with database icon) ⭐
   - **This is what you requested!**
   - Opens the UDF Catalog Search page filtered for NiFi
   - URL: `http://localhost:5173/udf-catalog/search?platform=NiFi`
   - Shows all NiFi processors and their metadata

## Technical Details

### Default NiFi Configuration
- **Version**: 1.12.1 (defined in `ContainerModels.cs`)
- **HTTP Port**: 8080 (auto-incremented for additional containers)
- **HTTPS Port**: 8443 (auto-incremented for additional containers)
- **Credentials**: 
  - Username: `admin`
  - Password: `ctsBtRBKHRAx69EqUghvvgEvjnaLjFEB`

### API Endpoints Used
- `POST /api/containers/nifi` - Create container
- `GET /api/containers` - List all containers
- `GET /api/containers/{id}/health` - Check container health
- `GET /api/containers/{id}/logs/stream` - Stream progress (SSE)

### Frontend Components
- **NewItemPanel.tsx** - Shows available items to create
- **ContainerCreationProgress.tsx** - Draggable progress modal with SSE streaming
- **WorkspaceCanvas.tsx** - Main workspace view with container table

### Container Labels
All managed containers have these Docker labels:
```json
{
  "managed-by": "nifi-metadata-platform",
  "tool-type": "nifi",
  "workspace": "w1"
}
```

## Testing via API (Alternative)

You can also create containers programmatically:

```powershell
$body = @{
    name = "my-nifi-container"
    workspace = "w1"
    version = "1.12.1"
    httpPort = 8082
    httpsPort = 8445
} | ConvertTo-Json

Invoke-WebRequest -Uri "http://localhost:5000/api/containers/nifi" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"
```

## Troubleshooting

### Container Not Showing in Workspace
- Check workspace filter: Container must have `workspace: w1` label
- Refresh the page: Containers auto-refresh every 10 seconds
- Check API: `curl http://localhost:5000/api/containers`

### Progress Bar Stuck
- Check Docker: `docker ps -a` to see container status
- Check logs: `docker logs <container-name>`
- NiFi takes 1-2 minutes to fully start up

### Port Conflicts
- Each container needs unique ports
- Default ports: 8080/8443
- API auto-assigns ports if not specified
- Check used ports: `docker ps --format "{{.Names}}\t{{.Ports}}"`

## Next Steps

1. **Create Multiple Containers**: Test creating 2-3 NiFi containers in workspace w1
2. **Monitor Progress**: Watch the real-time progress bar and logs
3. **Verify Display**: Check that all containers appear in the workspace table
4. **Test Actions**: Click "Open in UDF" button to navigate to the catalog
5. **Check Health**: Wait for status to change from "Starting..." to "Ready"

## Related Files

- Backend:
  - `src/Presentation/NiFiMetadataPlatform.API/Controllers/ContainersController.cs`
  - `src/Presentation/NiFiMetadataPlatform.API/Services/DockerContainerService.cs`
  - `src/Presentation/NiFiMetadataPlatform.API/Models/ContainerModels.cs`

- Frontend:
  - `src/pages/WorkspaceCanvas.tsx`
  - `src/components/NewItemPanel.tsx`
  - `src/components/ContainerCreationProgress.tsx`
  - `src/services/containerService.ts`
