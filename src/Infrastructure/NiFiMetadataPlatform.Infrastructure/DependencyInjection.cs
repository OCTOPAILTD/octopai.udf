using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NiFiMetadataPlatform.Application.Interfaces;
using NiFiMetadataPlatform.Infrastructure.Configuration;
using NiFiMetadataPlatform.Infrastructure.Persistence.ArangoDB;
using NiFiMetadataPlatform.Infrastructure.Persistence.OpenSearch;
using OpenSearch.Client;
using OpenSearch.Net;

namespace NiFiMetadataPlatform.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure OpenSearch
        var openSearchSettings = configuration.GetSection("OpenSearch").Get<OpenSearchSettings>() ?? new OpenSearchSettings();
        services.AddSingleton(openSearchSettings);

        var connectionPool = new StaticConnectionPool(
            openSearchSettings.Urls.Select(url => new Uri(url)));

        var settings = new ConnectionSettings(connectionPool)
            .DefaultIndex(openSearchSettings.IndexName)
            .RequestTimeout(TimeSpan.FromSeconds(openSearchSettings.ConnectionTimeout))
            .MaximumRetries(openSearchSettings.MaxRetries);

        if (!string.IsNullOrWhiteSpace(openSearchSettings.Username) &&
            !string.IsNullOrWhiteSpace(openSearchSettings.Password))
        {
            settings.BasicAuthentication(openSearchSettings.Username, openSearchSettings.Password);
        }

        var client = new OpenSearchClient(settings);
        services.AddSingleton<IOpenSearchClient>(client);

        // Configure ArangoDB
        var arangoSettings = configuration.GetSection("ArangoDB").Get<ArangoDbSettings>() ?? new ArangoDbSettings();
        services.AddSingleton(arangoSettings);

        // Register repositories
        services.AddScoped<ISearchRepository, OpenSearchRepository>();
        services.AddScoped<IGraphRepository, ArangoDbRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, NiFiMetadataPlatform.Infrastructure.Persistence.UnitOfWork>();

        return services;
    }
}
