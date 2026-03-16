using Microsoft.AspNetCore.Mvc;
using NiFiMetadataPlatform.API.Models;
using NiFiMetadataPlatform.API.Services;

namespace NiFiMetadataPlatform.API.Controllers;

/// <summary>
/// Container management API controller.
/// </summary>
[ApiController]
[Route("api/containers")]
[Produces("application/json")]
public sealed class ContainersController : ControllerBase
{
    private readonly IDockerContainerService _containerService;
    private readonly ILogger<ContainersController> _logger;

    public ContainersController(
        IDockerContainerService containerService,
        ILogger<ContainersController> logger)
    {
        _containerService = containerService;
        _logger = logger;
    }

    /// <summary>
    /// List all managed containers.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListContainers()
    {
        try
        {
            var containers = await _containerService.ListContainersAsync();
            return Ok(new { containers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list containers");
            return StatusCode(500, new { error = "Failed to list containers", message = ex.Message });
        }
    }

    /// <summary>
    /// Get container by ID.
    /// </summary>
    [HttpGet("{containerId}")]
    public async Task<IActionResult> GetContainer(string containerId)
    {
        try
        {
            var container = await _containerService.GetContainerAsync(containerId);
            if (container == null)
            {
                return NotFound(new { error = "Container not found" });
            }
            return Ok(container);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container: {ContainerId}", containerId);
            return StatusCode(500, new { error = "Failed to get container", message = ex.Message });
        }
    }

    /// <summary>
    /// Get container health.
    /// </summary>
    [HttpGet("{containerId}/health")]
    public async Task<IActionResult> GetContainerHealth(string containerId)
    {
        try
        {
            var health = await _containerService.GetContainerHealthAsync(containerId);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container health: {ContainerId}", containerId);
            return StatusCode(500, new { error = "Failed to get container health", message = ex.Message });
        }
    }

    /// <summary>
    /// Stream container startup progress via Server-Sent Events.
    /// </summary>
    [HttpGet("{containerId}/logs/stream")]
    public async Task StreamContainerProgress(string containerId)
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        try
        {
            await SendProgressUpdate(20, "Container created, checking status...", null);
            await Task.Delay(500);

            var container = await _containerService.GetContainerAsync(containerId);
            if (container == null)
            {
                await SendProgressUpdate(0, "Container not found", "error");
                return;
            }

            await SendProgressUpdate(40, "Container found, streaming logs...", null);
            
            // Stream container logs in parallel with health checks
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMinutes(3);
            var lastProgress = 40;
            var isReady = false;
            var logStreamCts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);

            // Start streaming logs from the container in background
            var logTask = Task.Run(async () =>
            {
                try
                {
                    await _containerService.StreamContainerLogsAsync(containerId, async (logLine) =>
                    {
                        // Send each log line to the client
                        await SendLogMessage(logLine);
                    }, logStreamCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when we cancel the stream
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Log streaming ended for container {ContainerId}", containerId);
                }
            }, logStreamCts.Token);

            // Poll health in parallel
            // For NiFi, we need to wait longer as "running" doesn't mean NiFi is ready
            var minWaitTime = TimeSpan.FromSeconds(60); // Wait at least 60 seconds for NiFi to initialize
            
            while (DateTime.UtcNow - startTime < timeout && !isReady)
            {
                var health = await _containerService.GetContainerHealthAsync(containerId);
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                var progressPercent = Math.Min(40 + (int)(elapsed / timeout.TotalSeconds * 50), 90);

                if (progressPercent > lastProgress)
                {
                    await SendProgressUpdate(progressPercent, $"Container starting... ({health.Status})", null);
                    lastProgress = progressPercent;
                }

                // Check if container is healthy or running, but only after minimum wait time
                // This ensures we capture the full NiFi startup logs (NAR expansion, etc.)
                if ((health.Status == "healthy" || health.Status == "running") && 
                    DateTime.UtcNow - startTime >= minWaitTime)
                {
                    isReady = true;
                    await SendProgressUpdate(100, "Container is ready!", "success");
                    break;
                }

                await Task.Delay(2000);
            }

            if (!isReady)
            {
                // Timeout - but container might still be starting
                await SendProgressUpdate(90, "Container is starting (may take a few more minutes)...", "success");
            }

            // Continue streaming logs for a few more seconds after container is ready
            await Task.Delay(3000);
            
            // Cancel log streaming
            logStreamCts.Cancel();
            
            // Wait for log task to complete (with timeout)
            try
            {
                await Task.WhenAny(logTask, Task.Delay(2000));
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream progress for container: {ContainerId}", containerId);
            await SendProgressUpdate(0, $"Error: {ex.Message}", "error");
        }
    }

    private async Task SendLogMessage(string logLine)
    {
        var data = new
        {
            message = logLine,
            type = "info",
            isLog = true
        };

        var json = System.Text.Json.JsonSerializer.Serialize(data);
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }

    private async Task SendProgressUpdate(int progress, string message, string? status)
    {
        var data = new
        {
            progress,
            step = message,
            message,
            status,
            type = status == "error" ? "error" : status == "success" ? "success" : "info"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(data);
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }

    /// <summary>
    /// Create a NiFi container.
    /// </summary>
    [HttpPost("nifi")]
    public async Task<IActionResult> CreateNiFiContainer([FromBody] CreateNiFiContainerRequest request)
    {
        try
        {
            _logger.LogInformation("Creating NiFi container: {Name}", request.Name);
            var container = await _containerService.CreateNiFiContainerAsync(request);
            return Ok(new { success = true, container });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create NiFi container");
            return StatusCode(500, new { success = false, error = "Failed to create NiFi container", message = ex.Message });
        }
    }

    /// <summary>
    /// Create a Kafka container.
    /// </summary>
    [HttpPost("kafka")]
    public async Task<IActionResult> CreateKafkaContainer([FromBody] CreateKafkaContainerRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Kafka container: {Name}", request.Name);
            var container = await _containerService.CreateKafkaContainerAsync(request);
            return Ok(new { success = true, container });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Kafka container");
            return StatusCode(500, new { success = false, error = "Failed to create Kafka container", message = ex.Message });
        }
    }

    /// <summary>
    /// Create a Hive container.
    /// </summary>
    [HttpPost("hive")]
    public async Task<IActionResult> CreateHiveContainer([FromBody] CreateHiveContainerRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Hive container: {Name}", request.Name);
            var container = await _containerService.CreateHiveContainerAsync(request);
            return Ok(new { success = true, container });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Hive container");
            return StatusCode(500, new { success = false, error = "Failed to create Hive container", message = ex.Message });
        }
    }

    /// <summary>
    /// Create a Trino container.
    /// </summary>
    [HttpPost("trino")]
    public async Task<IActionResult> CreateTrinoContainer([FromBody] CreateTrinoContainerRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Trino container: {Name}", request.Name);
            var container = await _containerService.CreateTrinoContainerAsync(request);
            return Ok(new { success = true, container });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Trino container");
            return StatusCode(500, new { success = false, error = "Failed to create Trino container", message = ex.Message });
        }
    }

    /// <summary>
    /// Create an Impala container.
    /// </summary>
    [HttpPost("impala")]
    public async Task<IActionResult> CreateImpalaContainer([FromBody] CreateImpalaContainerRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Impala container: {Name}", request.Name);
            var container = await _containerService.CreateImpalaContainerAsync(request);
            return Ok(new { success = true, container });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Impala container");
            return StatusCode(500, new { success = false, error = "Failed to create Impala container", message = ex.Message });
        }
    }

    /// <summary>
    /// Create an HBase container.
    /// </summary>
    [HttpPost("hbase")]
    public async Task<IActionResult> CreateHBaseContainer([FromBody] CreateHBaseContainerRequest request)
    {
        try
        {
            _logger.LogInformation("Creating HBase container: {Name}", request.Name);
            var container = await _containerService.CreateHBaseContainerAsync(request);
            return Ok(new { success = true, container });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create HBase container");
            return StatusCode(500, new { success = false, error = "Failed to create HBase container", message = ex.Message });
        }
    }

    /// <summary>
    /// Create a DataHub container.
    /// </summary>
    [HttpPost("datahub")]
    public async Task<IActionResult> CreateDataHubContainer([FromBody] CreateDataHubContainerRequest request)
    {
        try
        {
            _logger.LogInformation("Creating DataHub container: {Name}", request.Name);
            var container = await _containerService.CreateDataHubContainerAsync(request);
            return Ok(new { success = true, container });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create DataHub container");
            return StatusCode(500, new { success = false, error = "Failed to create DataHub container", message = ex.Message });
        }
    }

    /// <summary>
    /// Start a container.
    /// </summary>
    [HttpPost("{containerId}/start")]
    public async Task<IActionResult> StartContainer(string containerId)
    {
        try
        {
            var success = await _containerService.StartContainerAsync(containerId);
            return Ok(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start container: {ContainerId}", containerId);
            return StatusCode(500, new { success = false, error = "Failed to start container", message = ex.Message });
        }
    }

    /// <summary>
    /// Stop a container.
    /// </summary>
    [HttpPost("{containerId}/stop")]
    public async Task<IActionResult> StopContainer(string containerId)
    {
        try
        {
            var success = await _containerService.StopContainerAsync(containerId);
            return Ok(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container: {ContainerId}", containerId);
            return StatusCode(500, new { success = false, error = "Failed to stop container", message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a container.
    /// </summary>
    [HttpDelete("{containerId}")]
    public async Task<IActionResult> RemoveContainer(string containerId)
    {
        try
        {
            var success = await _containerService.RemoveContainerAsync(containerId);
            return Ok(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container: {ContainerId}", containerId);
            return StatusCode(500, new { success = false, error = "Failed to remove container", message = ex.Message });
        }
    }
}
