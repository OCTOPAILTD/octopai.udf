using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NiFiMetadataPlatform.Application;
using NiFiMetadataPlatform.Infrastructure;
using NiFiMetadataPlatform.API.Services;
using Prometheus;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting NiFi Metadata Platform API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontendAndPython", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:5000",
                    "http://127.0.0.1:5173",
                    "http://127.0.0.1:5000")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "NiFi Metadata Platform API",
            Version = "v1",
            Description = "Enterprise-grade metadata management for Apache NiFi"
        });
    });

    // Register HttpClient for NiFi API calls
    builder.Services.AddHttpClient();

    // Register application and infrastructure services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Register Docker container service
    builder.Services.AddSingleton<IDockerContainerService, DockerContainerService>();

    // Register NiFi metadata ingestion service
    builder.Services.AddScoped<INiFiMetadataIngestionService, NiFiMetadataIngestionService>();

    // Register GSP SQL Parser service
    builder.Services.AddScoped<IGSPSqlParserService, GSPSqlParserService>();

    // Register NiFi Schema Extractor service
    builder.Services.AddScoped<INiFiSchemaExtractor, NiFiSchemaExtractor>();

    // Register Column Lineage Mapper service
    builder.Services.AddScoped<IColumnLineageMapper, ColumnLineageMapper>();

    // Register NiFi metadata monitor background service
    builder.Services.AddHostedService<NiFiMetadataMonitorService>();

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();

    app.UseHttpMetrics();

    app.UseCors("AllowFrontendAndPython");

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapMetrics();

    Log.Information("API started successfully");

    await app.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "API terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
