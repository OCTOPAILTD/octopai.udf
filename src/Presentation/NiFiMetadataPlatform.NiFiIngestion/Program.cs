using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NiFiMetadataPlatform.NiFiIngestion.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting NiFi Metadata Ingestion Service");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, loggerConfiguration) =>
        loggerConfiguration.ReadFrom.Configuration(builder.Configuration));

    // Register HTTP client for NiFi API
    builder.Services.AddHttpClient("NiFiClient", client =>
    {
        var nifiUrl = builder.Configuration["NiFi:Url"] ?? "http://localhost:8080";
        client.BaseAddress = new Uri(nifiUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // Register HTTP client for Metadata API
    builder.Services.AddHttpClient("MetadataApiClient", client =>
    {
        var apiUrl = builder.Configuration["MetadataApi:Url"] ?? "http://localhost:5000";
        client.BaseAddress = new Uri(apiUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // Register the background service
    builder.Services.AddHostedService<NiFiIngestionWorker>();

    var host = builder.Build();

    Log.Information("NiFi Ingestion Service started successfully");

    await host.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "NiFi Ingestion Service terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
