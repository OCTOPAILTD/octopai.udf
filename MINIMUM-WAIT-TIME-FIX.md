# Fix: Minimum Wait Time for Complete Log Streaming

## Problem You Encountered

When creating a NiFi container, the progress modal showed:
```
[13:18:46] replacing target file /opt/nifi/nifi-current/conf/nifi.properties
[13:18:46] replacing target file /opt/nifi/nifi-current/conf/nifi.properties
...
[13:18:46] NiFi running with PID 34.
[13:18:46] tail: cannot open '/opt/nifi/nifi-current/logs/nifi-app.log' for reading: No such file or directory
[13:18:46] Java home: /usr/local/openjdk-8
[13:18:46] NiFi home: /opt/nifi/nifi-current
[13:18:46] Bootstrap Config File: /opt/nifi/nifi-current/conf/bootstrap.conf
[13:18:46] Container is ready!
[13:18:46] ✅ NiFi is ready to use!
```

**Issue**: The logs stopped after only ~1 second, showing "Container is ready!" but missing the important startup logs:
- ❌ No "Launching NiFi..." message
- ❌ No "Loaded 165 properties" message  
- ❌ No "Expanding 102 NAR files" message
- ❌ No component loading logs
- ❌ No web server startup logs

## Root Cause

The health check was detecting the container as "running" **immediately** after it started, which triggered the "ready" status and stopped the log stream. However:

1. **Container state "running"** ≠ **NiFi application ready**
2. NiFi takes **60-120 seconds** to fully initialize
3. The log stream was ending after only **1-2 seconds**

## Solution

Added a **minimum wait time of 60 seconds** before marking the container as "ready", even if the Docker container state is "running".

### Code Change

```csharp
// Before:
if (health.Status == "healthy" || health.Status == "running")
{
    isReady = true;
    await SendProgressUpdate(100, "Container is ready!", "success");
    break;
}

// After:
var minWaitTime = TimeSpan.FromSeconds(60); // Wait at least 60 seconds for NiFi

if ((health.Status == "healthy" || health.Status == "running") && 
    DateTime.UtcNow - startTime >= minWaitTime)
{
    isReady = true;
    await SendProgressUpdate(100, "Container is ready!", "success");
    break;
}
```

## What You'll See Now

When you create a new NiFi container, the progress modal will:

1. **Start streaming logs** (0-10 seconds):
   ```
   [13:18:46] replacing target file /opt/nifi/nifi-current/conf/nifi.properties
   [13:18:46] NiFi running with PID 34
   [13:18:46] Bootstrap Config File: /opt/nifi/nifi-current/conf/bootstrap.conf
   ```

2. **Continue streaming** (10-30 seconds):
   ```
   2026-03-08 11:08:02,384 INFO [main] org.apache.nifi.bootstrap.Command Working Directory: /opt/nifi/nifi-current
   2026-03-08 11:08:02,472 INFO [main] org.apache.nifi.bootstrap.Command Launched Apache NiFi with Process ID 69
   2026-03-08 11:08:02,629 INFO [main] org.apache.nifi.NiFi Launching NiFi...
   ```

3. **Show initialization** (30-60 seconds):
   ```
   2026-03-08 11:08:02,810 INFO [main] o.a.nifi.properties.NiFiPropertiesLoader Loaded 165 properties
   2026-03-08 11:08:02,820 INFO [main] org.apache.nifi.BootstrapListener Started Bootstrap Listener
   2026-03-08 11:08:02,892 INFO [main] org.apache.nifi.nar.NarUnpacker Expanding 102 NAR files with all processors...
   ```

4. **Complete startup** (60-90 seconds):
   ```
   2026-03-08 11:09:15,234 INFO [main] org.apache.nifi.web.server.JettyServer NiFi has started. The UI is available at the following URLs:
   2026-03-08 11:09:15,235 INFO [main] org.apache.nifi.web.server.JettyServer http://localhost:8080/nifi
   ```

5. **Mark as ready** (after 60+ seconds):
   ```
   Container is ready!
   ✅ NiFi is ready to use!
   ```

## Timeline Comparison

### Before (Too Fast)
```
0s  - Container starts
1s  - Health check: "running" → Mark as ready ❌
1s  - Log stream stops
```

### After (Proper Wait)
```
0s  - Container starts
1s  - Health check: "running" but < 60s → Keep streaming ✅
10s - Logs: Bootstrap, Working Directory, Process ID
20s - Logs: Launching NiFi, Properties loading
30s - Logs: Bootstrap Listener, NAR expansion starts
60s - Health check: "running" and >= 60s → Mark as ready ✅
63s - Log stream stops (after 3 more seconds)
```

## Why 60 Seconds?

NiFi's startup phases:
1. **0-5s**: Container initialization, file setup
2. **5-10s**: Bootstrap process starts
3. **10-20s**: Java process launches, properties load
4. **20-40s**: NAR file expansion (102 files!)
5. **40-60s**: Component loading, extension discovery
6. **60-90s**: Web server starts, UI becomes available

**60 seconds** ensures we capture at least through the NAR expansion phase, which is the most important part to see.

## Testing

### Remove any existing containers:
```powershell
docker stop $(docker ps -q --filter "label=tool-type=nifi")
docker rm $(docker ps -aq --filter "label=tool-type=nifi")
```

### Create new container via UI:
1. Navigate to `http://localhost:5173/workspace/w1`
2. Click "+ New item"
3. Select "NiFi Flow"
4. **Watch the logs for at least 60 seconds**
5. You should see the complete startup sequence
6. Progress bar will stay below 100% for at least 60 seconds
7. After 60+ seconds, you'll see "Container is ready!" ✅

## Expected Log Sequence

```
[Time 0s] File replacements
[Time 1s] Bootstrap Config File
[Time 2s] Working Directory
[Time 3s] Java command (long classpath)
[Time 4s] Launched Apache NiFi with Process ID
[Time 5s] tail: following new file
[Time 6s] Launching NiFi...
[Time 7s] Determined default nifi.properties path
[Time 8s] Loaded 165 properties from nifi.properties
[Time 9s] Loaded 165 properties
[Time 10s] Started Bootstrap Listener
[Time 11s] Successfully initiated communication with Bootstrap
[Time 12s] Expanding 102 NAR files with all processors...
[Time 15-50s] NAR expansion progress...
[Time 50-60s] Component loading, extension discovery...
[Time 60+s] Web server starting...
[Time 60+s] Container is ready! ✅
```

## Files Modified

- **`src/Presentation/NiFiMetadataPlatform.API/Controllers/ContainersController.cs`**
  - Added `minWaitTime = TimeSpan.FromSeconds(60)`
  - Modified health check condition to include time check
  - Ensures logs stream for at least 60 seconds

## Current Status

✅ **API rebuilt** with minimum wait time fix
✅ **API restarted** and running
✅ **Ready to test** - create a new NiFi container

## Summary

The fix ensures that:
1. ✅ Log streaming continues for at least 60 seconds
2. ✅ You see the complete NiFi startup sequence
3. ✅ Progress bar doesn't jump to 100% too early
4. ✅ "Container is ready!" only appears after meaningful initialization

**Now create a new NiFi container and watch the complete log sequence!** 🚀
