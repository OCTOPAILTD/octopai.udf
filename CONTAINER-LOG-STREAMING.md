# Container Log Streaming Feature

## Overview
The progress bar window now displays **real-time Docker container logs** during NiFi (and other container) creation, giving you full visibility into the startup process.

## What You'll See

When you create a new NiFi container through the UI, the progress modal will now show:

1. **Progress Bar** (0-100%)
2. **Current Step** (e.g., "Container starting...")
3. **Real-Time Logs** from the Docker container, including:
   - Bootstrap initialization
   - Java process startup
   - NiFi configuration loading
   - NAR file expansion
   - Web server startup
   - Health check status

## Example Logs You'll See

```
Bootstrap Config File: /opt/nifi/nifi-current/conf/bootstrap.conf

2026-03-08 10:47:58,043 INFO [main] org.apache.nifi.bootstrap.Command Starting Apache NiFi...
2026-03-08 10:47:58,043 INFO [main] org.apache.nifi.bootstrap.Command Working Directory: /opt/nifi/nifi-current
2026-03-08 10:47:58,752 INFO [main] org.apache.nifi.NiFi Launching NiFi...
2026-03-08 10:47:59,956 INFO [main] org.apache.nifi.security.kms.CryptoUtils Determined default nifi.properties path
2026-03-08 10:47:59,959 INFO [main] o.a.nifi.properties.NiFiPropertiesLoader Loaded 165 properties
2026-03-08 10:48:00,032 INFO [main] org.apache.nifi.BootstrapListener Started Bootstrap Listener
2026-03-08 10:48:01,775 INFO [main] org.apache.nifi.nar.NarUnpacker Expanding 102 NAR files with all processors...
```

## How It Works

### Backend (C# API)

1. **Server-Sent Events (SSE)**: The API uses SSE to stream data to the frontend
   - Endpoint: `GET /api/containers/{containerId}/logs/stream`
   - Content-Type: `text/event-stream`

2. **Docker Log Streaming**: 
   - Connects to Docker daemon via Docker.DotNet library
   - Reads container logs using `GetContainerLogsAsync`
   - Parses Docker's multiplexed stream format (8-byte header + payload)
   - Sends each log line to the frontend in real-time

3. **Parallel Health Monitoring**:
   - While streaming logs, also polls container health
   - Updates progress bar based on elapsed time
   - Detects when container is "ready" (healthy/running)

### Frontend (React/TypeScript)

1. **ContainerCreationProgress Component**:
   - Opens EventSource connection to SSE endpoint
   - Listens for log messages and progress updates
   - Displays logs in scrollable window
   - Auto-scrolls to latest log entry

2. **Log Display**:
   - Each log line is timestamped
   - Color-coded by type (info/success/error)
   - Draggable modal window
   - Persists until container is ready or error occurs

## Technical Implementation

### Backend Code

```csharp
// ContainersController.cs
[HttpGet("{containerId}/logs/stream")]
public async Task StreamContainerProgress(string containerId)
{
    Response.Headers.Add("Content-Type", "text/event-stream");
    
    // Start streaming logs from Docker
    var logTask = _containerService.StreamContainerLogsAsync(
        containerId, 
        async (logLine) => {
            await SendLogMessage(logLine);
        }, 
        HttpContext.RequestAborted
    );
    
    // Poll health in parallel
    while (!isReady) {
        var health = await _containerService.GetContainerHealthAsync(containerId);
        // Update progress...
    }
}

// DockerContainerService.cs
public async Task StreamContainerLogsAsync(
    string containerId, 
    Func<string, Task> onLogLine, 
    CancellationToken cancellationToken)
{
    var stream = await _dockerClient.Containers.GetContainerLogsAsync(
        containerId,
        new ContainerLogsParameters {
            ShowStdout = true,
            ShowStderr = true,
            Follow = true,
            Tail = "all"
        },
        cancellationToken
    );
    
    // Parse Docker log format and send each line
    while (!cancellationToken.IsCancellationRequested) {
        // Read 8-byte header
        var headerBytes = new byte[8];
        await stream.ReadAsync(headerBytes, 0, 8, cancellationToken);
        
        // Extract payload size (bytes 4-7, big-endian)
        var payloadSize = (headerBytes[4] << 24) | 
                         (headerBytes[5] << 16) | 
                         (headerBytes[6] << 8) | 
                         headerBytes[7];
        
        // Read payload
        var payloadBytes = new byte[payloadSize];
        await stream.ReadAsync(payloadBytes, 0, payloadSize, cancellationToken);
        
        var logLine = Encoding.UTF8.GetString(payloadBytes).TrimEnd();
        await onLogLine(logLine);
    }
}
```

### Frontend Code

```typescript
// ContainerCreationProgress.tsx
useEffect(() => {
    if (!containerId) return;
    
    const eventSource = new EventSource(
        `${getApiUrl()}/api/containers/${containerId}/logs/stream`
    );
    
    eventSource.onmessage = (event) => {
        const data = JSON.parse(event.data);
        
        if (data.message) {
            addLog(data.message, data.type || 'info');
        }
        
        if (data.progress) {
            setProgress(data.progress);
        }
        
        if (data.status === 'success') {
            setStatus('success');
            setProgress(100);
            eventSource.close();
        }
    };
    
    return () => eventSource.close();
}, [containerId]);
```

## Testing the Feature

### 1. Create a New Container via UI

1. Navigate to `http://localhost:5173/workspace/w1`
2. Click "+ New item"
3. Select "NiFi Flow"
4. Watch the progress modal appear with real-time logs

### 2. Test via API (Manual)

```powershell
# Create a container
$body = @{
    name = "test-nifi"
    workspace = "w1"
    version = "1.12.1"
    httpPort = 8085
    httpsPort = 8446
} | ConvertTo-Json

$response = Invoke-WebRequest -Uri "http://localhost:5000/api/containers/nifi" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"

$container = ($response.Content | ConvertFrom-Json).container

# Stream logs (in browser or curl)
curl http://localhost:5000/api/containers/$($container.id)/logs/stream
```

### 3. Expected Behavior

- **First 10 seconds**: Progress bar at 20-40%, showing "Container created, streaming logs..."
- **10-60 seconds**: Logs appear showing NiFi bootstrap, Java startup, NAR expansion
- **60-120 seconds**: More logs showing web server initialization, component loading
- **When ready**: Progress jumps to 100%, status changes to "success", modal can be closed

## Troubleshooting

### Logs Not Appearing

**Symptom**: Progress bar moves but no logs shown

**Causes**:
1. Container hasn't started yet (no logs to stream)
2. EventSource connection failed
3. Backend can't connect to Docker daemon

**Solutions**:
- Check browser console for EventSource errors
- Verify API logs: `docker logs nifi-metadata-api`
- Ensure Docker socket is mounted: `/var/run/docker.sock:/var/run/docker.sock`

### Progress Stuck at 40%

**Symptom**: Logs streaming but progress doesn't advance

**Cause**: Health check not detecting "ready" state

**Solutions**:
- NiFi takes 1-2 minutes to fully start
- Check if NiFi is actually starting: `docker logs <container-name>`
- Verify ports are not in use: `netstat -ano | findstr "8080"`

### "Connection Lost" Error

**Symptom**: EventSource closes prematurely

**Causes**:
1. API restarted during creation
2. Network timeout
3. Container creation failed

**Solutions**:
- Check API status: `docker ps --filter "name=nifi-metadata-api"`
- Review API logs for errors
- Verify container exists: `docker ps -a | findstr "test-nifi"`

## Performance Considerations

### Log Volume
- Docker logs can be verbose (especially NiFi with 102 NARs)
- Each log line is sent individually via SSE
- Frontend auto-scrolls to latest (may lag on slow connections)

### Network Usage
- SSE keeps HTTP connection open for 2-3 minutes
- Typical log volume: 50-200 lines during startup
- Each line: ~100-500 bytes
- Total: ~10-100 KB per container creation

### Browser Compatibility
- EventSource is supported in all modern browsers
- Falls back gracefully if connection fails
- Modal remains functional even without logs

## Future Enhancements

1. **Log Filtering**: Filter by log level (INFO, WARN, ERROR)
2. **Log Search**: Search within logs during streaming
3. **Download Logs**: Export logs to file
4. **Syntax Highlighting**: Color-code Java stack traces
5. **Progress Estimation**: ML-based ETA for container readiness
6. **Multi-Container**: Stream logs from multiple containers simultaneously

## Related Files

### Backend
- `src/Presentation/NiFiMetadataPlatform.API/Controllers/ContainersController.cs`
  - `StreamContainerProgress()` - SSE endpoint
  - `SendLogMessage()` - Helper to send log lines

- `src/Presentation/NiFiMetadataPlatform.API/Services/DockerContainerService.cs`
  - `StreamContainerLogsAsync()` - Docker log streaming implementation

- `src/Presentation/NiFiMetadataPlatform.API/Services/IDockerContainerService.cs`
  - Interface definition for log streaming

### Frontend
- `src/components/ContainerCreationProgress.tsx`
  - Progress modal with log display
  - EventSource connection management
  - Auto-scroll and drag functionality

- `src/services/containerService.ts`
  - Container creation API calls

- `src/pages/WorkspaceCanvas.tsx`
  - Triggers container creation
  - Opens progress modal

## Docker Log Format

Docker uses a multiplexed stream format for container logs:

```
[8 bytes header][N bytes payload][8 bytes header][N bytes payload]...
```

**Header Structure** (8 bytes):
- Byte 0: Stream type (1=stdout, 2=stderr)
- Bytes 1-3: Reserved (padding)
- Bytes 4-7: Payload size (big-endian uint32)

**Example**:
```
01 00 00 00 00 00 00 2A [42 bytes of log data]
^^          ^^^^^^^^^^
stdout      size=42
```

Our implementation parses this format to extract clean log lines.

## Security Considerations

1. **Docker Socket Access**: API container needs `/var/run/docker.sock` mounted
2. **Log Sanitization**: No sensitive data filtering (passwords may appear in logs)
3. **Rate Limiting**: No rate limiting on SSE endpoint (could be DoS vector)
4. **CORS**: Ensure proper CORS headers for cross-origin requests

## Conclusion

The container log streaming feature provides real-time visibility into Docker container startup, making it easier to:
- **Debug** container creation issues
- **Monitor** progress of long-running startups
- **Understand** what's happening during initialization
- **Diagnose** failures immediately

The logs you see are the **exact same logs** you'd see with `docker logs <container>`, but streamed live to the UI as they're generated!
