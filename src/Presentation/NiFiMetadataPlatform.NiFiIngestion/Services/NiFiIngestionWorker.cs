using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NiFiMetadataPlatform.NiFiIngestion.Services;

/// <summary>
/// Background service that polls NiFi for metadata changes and sends them to the Metadata API.
/// </summary>
public sealed class NiFiIngestionWorker : BackgroundService
{
    private readonly ILogger<NiFiIngestionWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, string> _lastKnownHashes = new();
    private readonly int _pollingIntervalSeconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="NiFiIngestionWorker"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="configuration">The configuration.</param>
    public NiFiIngestionWorker(
        ILogger<NiFiIngestionWorker> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _pollingIntervalSeconds = configuration.GetValue<int>("NiFi:PollingIntervalSeconds", 10);
    }

    /// <summary>
    /// Executes the background service.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "NiFi Ingestion Worker started. Polling interval: {Interval} seconds",
            _pollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollNiFiMetadataAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling NiFi metadata");
            }

            await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("NiFi Ingestion Worker stopped");
    }

    private async Task PollNiFiMetadataAsync(CancellationToken cancellationToken)
    {
        var nifiClient = _httpClientFactory.CreateClient("NiFiClient");

        try
        {
            // Get the root process group
            var response = await nifiClient.GetAsync(
                "/nifi-api/flow/process-groups/root",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get NiFi flow. Status: {StatusCode}",
                    response.StatusCode);
                return;
            }

            var flowData = await response.Content.ReadAsStringAsync(cancellationToken);
            var currentHash = ComputeHash(flowData);

            // Check if the flow has changed
            if (_lastKnownHashes.TryGetValue("root", out var lastHash) && lastHash == currentHash)
            {
                _logger.LogDebug("No changes detected in NiFi flow");
                return;
            }

            _logger.LogInformation("Changes detected in NiFi flow, processing metadata");
            _lastKnownHashes["root"] = currentHash;

            // Parse and send metadata to the API
            await ProcessFlowMetadataAsync(flowData, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while polling NiFi");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "NiFi polling request timed out");
        }
    }

    private async Task ProcessFlowMetadataAsync(string flowData, CancellationToken cancellationToken)
    {
        try
        {
            var flowDocument = JsonDocument.Parse(flowData);
            var processGroupFlow = flowDocument.RootElement.GetProperty("processGroupFlow");
            var flow = processGroupFlow.GetProperty("flow");

            // Extract processors
            if (flow.TryGetProperty("processors", out var processors))
            {
                foreach (var processor in processors.EnumerateArray())
                {
                    await SendProcessorMetadataAsync(processor, cancellationToken);
                }
            }

            // Extract connections
            if (flow.TryGetProperty("connections", out var connections))
            {
                foreach (var connection in connections.EnumerateArray())
                {
                    await SendConnectionMetadataAsync(connection, cancellationToken);
                }
            }

            // Extract process groups
            if (flow.TryGetProperty("processGroups", out var processGroups))
            {
                foreach (var processGroup in processGroups.EnumerateArray())
                {
                    await SendProcessGroupMetadataAsync(processGroup, cancellationToken);
                }
            }

            _logger.LogInformation("Successfully processed NiFi metadata");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing flow metadata");
        }
    }

    private async Task SendProcessorMetadataAsync(
        JsonElement processor,
        CancellationToken cancellationToken)
    {
        try
        {
            var component = processor.GetProperty("component");
            var id = component.GetProperty("id").GetString();
            var name = component.GetProperty("name").GetString();
            var type = component.GetProperty("type").GetString();

            var metadata = new
            {
                id,
                name,
                type,
                entityType = "processor",
                timestamp = DateTime.UtcNow,
                properties = component.TryGetProperty("config", out var config)
                    ? JsonSerializer.Serialize(config)
                    : "{}"
            };

            await SendToMetadataApiAsync("/api/metadata/ingest", metadata, cancellationToken);

            _logger.LogDebug("Sent processor metadata: {ProcessorName} ({ProcessorId})", name, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending processor metadata");
        }
    }

    private async Task SendConnectionMetadataAsync(
        JsonElement connection,
        CancellationToken cancellationToken)
    {
        try
        {
            var component = connection.GetProperty("component");
            var id = component.GetProperty("id").GetString();
            var name = component.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString()
                : "Unnamed Connection";

            var sourceId = component.GetProperty("source").GetProperty("id").GetString();
            var destinationId = component.GetProperty("destination").GetProperty("id").GetString();

            var metadata = new
            {
                id,
                name,
                entityType = "connection",
                sourceId,
                destinationId,
                timestamp = DateTime.UtcNow
            };

            await SendToMetadataApiAsync("/api/metadata/ingest", metadata, cancellationToken);

            _logger.LogDebug("Sent connection metadata: {ConnectionName} ({ConnectionId})", name, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending connection metadata");
        }
    }

    private async Task SendProcessGroupMetadataAsync(
        JsonElement processGroup,
        CancellationToken cancellationToken)
    {
        try
        {
            var component = processGroup.GetProperty("component");
            var id = component.GetProperty("id").GetString();
            var name = component.GetProperty("name").GetString();

            var metadata = new
            {
                id,
                name,
                entityType = "processGroup",
                timestamp = DateTime.UtcNow
            };

            await SendToMetadataApiAsync("/api/metadata/ingest", metadata, cancellationToken);

            _logger.LogDebug("Sent process group metadata: {GroupName} ({GroupId})", name, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending process group metadata");
        }
    }

    private async Task SendToMetadataApiAsync(
        string endpoint,
        object data,
        CancellationToken cancellationToken)
    {
        var apiClient = _httpClientFactory.CreateClient("MetadataApiClient");

        try
        {
            var response = await apiClient.PostAsJsonAsync(endpoint, data, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Failed to send metadata to API. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode,
                    errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending metadata to API");
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
