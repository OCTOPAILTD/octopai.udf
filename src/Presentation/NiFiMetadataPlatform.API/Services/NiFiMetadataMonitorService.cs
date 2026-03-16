using NiFiMetadataPlatform.API.Models;

namespace NiFiMetadataPlatform.API.Services;

/// <summary>
/// Background service that monitors NiFi containers and automatically ingests metadata.
/// </summary>
public sealed class NiFiMetadataMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDockerContainerService _containerService;
    private readonly ILogger<NiFiMetadataMonitorService> _logger;
    private readonly Dictionary<string, DateTime> _lastIngestionTimes = new();
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);

    public NiFiMetadataMonitorService(
        IServiceProvider serviceProvider,
        IDockerContainerService containerService,
        ILogger<NiFiMetadataMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _containerService = containerService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NiFi Metadata Monitor Service started. Polling every {Interval} seconds", _pollingInterval.TotalSeconds);

        // Wait for services to be ready
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorNiFiContainersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring NiFi containers");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("NiFi Metadata Monitor Service stopped");
    }

    private async Task MonitorNiFiContainersAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get all running containers
            var containers = await _containerService.ListContainersAsync();
            
            foreach (var container in containers)
            {
                // Check if it's a NiFi container
                if (!IsNiFiContainer(container))
                {
                    continue;
                }

                var containerId = container.Id;
                var containerName = container.Name ?? containerId;

                // Check if container is running/healthy
                if (container.State != "running")
                {
                    _logger.LogDebug("Skipping non-running NiFi container: {ContainerName}", containerName);
                    continue;
                }

                // Check if we should ingest (not ingested recently)
                if (_lastIngestionTimes.TryGetValue(containerId, out var lastIngestion))
                {
                    var timeSinceLastIngestion = DateTime.UtcNow - lastIngestion;
                    if (timeSinceLastIngestion < TimeSpan.FromMinutes(5))
                    {
                        _logger.LogDebug("Skipping recent ingestion for {ContainerName} (last: {Time} ago)", 
                            containerName, timeSinceLastIngestion);
                        continue;
                    }
                }

                _logger.LogInformation("Ingesting metadata from NiFi container: {ContainerName} ({ContainerId})", 
                    containerName, containerId);

                // Create a scope for the ingestion service
                using var scope = _serviceProvider.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<INiFiMetadataIngestionService>();

                try
                {
                    var count = await ingestionService.IngestFromContainerAsync(containerId, cancellationToken);
                    _lastIngestionTimes[containerId] = DateTime.UtcNow;
                    
                    _logger.LogInformation("Successfully ingested {Count} entities from {ContainerName}", 
                        count, containerName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest metadata from {ContainerName}", containerName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MonitorNiFiContainersAsync");
        }
    }

    private bool IsNiFiContainer(ContainerInfo container)
    {
        try
        {
            if (container.Name?.Contains("nifi", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            return container.Image?.Contains("nifi", StringComparison.OrdinalIgnoreCase) ?? false;
        }
        catch
        {
            return false;
        }
    }
}
