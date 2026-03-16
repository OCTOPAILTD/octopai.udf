# Metadata Ingestion Extensibility Guide

This document explains how to add support for new data platforms to the NiFi Metadata Platform.

## Architecture Overview

The platform uses a pluggable architecture that allows adding new data sources without modifying the core system. Each data platform (NiFi, Trino, Kafka, etc.) implements a standard set of interfaces.

## Key Interfaces

### 1. IMetadataIngestionService

The main interface for ingestion services. Each platform implements this to:
- Connect to the source platform
- Discover metadata changes
- Send metadata to the central API

**Location:** `src/Core/NiFiMetadataPlatform.Domain/Interfaces/IMetadataIngestionService.cs`

### 2. IMetadataEntity

Common interface for all metadata entities, providing a unified structure regardless of source platform.

**Location:** `src/Core/NiFiMetadataPlatform.Domain/Interfaces/IMetadataEntity.cs`

### 3. IMetadataTransformer<TSource>

Interface for transforming platform-specific metadata to the common format.

**Location:** `src/Core/NiFiMetadataPlatform.Domain/Interfaces/IMetadataTransformer.cs`

## Adding a New Platform

### Step 1: Create the Ingestion Service Project

```bash
dotnet new worker -n NiFiMetadataPlatform.{PlatformName}Ingestion
cd NiFiMetadataPlatform.{PlatformName}Ingestion
dotnet add reference ../../Core/NiFiMetadataPlatform.Domain
dotnet add reference ../../Shared/NiFiMetadataPlatform.Contracts
```

### Step 2: Implement IMetadataIngestionService

```csharp
public class TrinoIngestionService : IMetadataIngestionService
{
    public string PlatformName => "Trino";
    public string SupportedVersion => "400+";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Start polling Trino metadata
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop polling
    }

    public async Task<int> DiscoverMetadataAsync(CancellationToken cancellationToken)
    {
        // Discover tables, views, schemas, etc.
        return entitiesDiscovered;
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
    {
        // Check connection to Trino
        return isHealthy;
    }
}
```

### Step 3: Create Platform-Specific Entity Models

```csharp
public class TrinoTable : IMetadataEntity
{
    public string Id { get; set; }
    public string Fqn { get; set; }
    public string Name { get; set; }
    public string EntityType => "table";
    public string Platform => "Trino";
    // ... other properties
}
```

### Step 4: Implement the Transformer

```csharp
public class TrinoMetadataTransformer : IMetadataTransformer<TrinoTableMetadata>
{
    public IMetadataEntity Transform(TrinoTableMetadata source)
    {
        return new TrinoTable
        {
            Id = source.TableId,
            Fqn = $"trino://{source.Catalog}/{source.Schema}/{source.TableName}",
            Name = source.TableName,
            // ... map other fields
        };
    }

    public IEnumerable<IMetadataEntity> TransformMany(IEnumerable<TrinoTableMetadata> sources)
    {
        return sources.Select(Transform);
    }

    public bool Validate(TrinoTableMetadata source)
    {
        return !string.IsNullOrEmpty(source.TableName) 
            && !string.IsNullOrEmpty(source.Schema);
    }
}
```

### Step 5: Create Dockerfile

Create `docker/Dockerfile.{platform}-ingestion`:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["src/Presentation/NiFiMetadataPlatform.{Platform}Ingestion/*.csproj", "src/Presentation/NiFiMetadataPlatform.{Platform}Ingestion/"]
# ... copy dependencies

RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NiFiMetadataPlatform.{Platform}Ingestion.dll"]
```

### Step 6: Add to Docker Compose

Add the new service to `docker/docker-compose.yml`:

```yaml
  trino-ingestion:
    build:
      context: ..
      dockerfile: docker/Dockerfile.trino-ingestion
    container_name: metadata-trino-ingestion
    environment:
      - DOTNET_ENVIRONMENT=Development
      - Trino__Url=http://trino-coordinator:8080
      - Trino__PollingIntervalSeconds=30
      - MetadataApi__Url=http://csharp-api:5000
    depends_on:
      csharp-api:
        condition: service_healthy
    networks:
      - nifi-metadata-network
    restart: unless-stopped
```

## Example Platforms to Add

### 1. Apache Kafka
- **Entities:** Topics, Schemas, Consumer Groups
- **Metadata Source:** Kafka Admin API, Schema Registry
- **FQN Format:** `kafka://{cluster}/{topic}`

### 2. Trino
- **Entities:** Catalogs, Schemas, Tables, Views
- **Metadata Source:** Trino System Tables
- **FQN Format:** `trino://{catalog}/{schema}/{table}`

### 3. Apache Hive
- **Entities:** Databases, Tables, Partitions
- **Metadata Source:** Hive Metastore API
- **FQN Format:** `hive://{database}/{table}`

### 4. Databricks
- **Entities:** Catalogs, Schemas, Tables, Notebooks
- **Metadata Source:** Databricks REST API
- **FQN Format:** `databricks://{workspace}/{catalog}/{schema}/{table}`

### 5. Apache Impala
- **Entities:** Databases, Tables, Views
- **Metadata Source:** Impala Catalog Service
- **FQN Format:** `impala://{database}/{table}`

## Best Practices

1. **Use Polling with Change Detection:** Implement hash-based change detection to avoid unnecessary API calls
2. **Implement Retry Logic:** Handle transient failures gracefully
3. **Log Extensively:** Use structured logging for debugging
4. **Health Checks:** Implement proper health checks for monitoring
5. **Configuration:** Use environment variables for configuration
6. **Error Handling:** Catch and log errors without crashing the service
7. **Rate Limiting:** Respect API rate limits of the source platform

## Testing

Create integration tests for your ingestion service:

```csharp
[Fact]
public async Task DiscoverMetadata_ShouldReturnEntities()
{
    // Arrange
    var service = new TrinoIngestionService(/* dependencies */);

    // Act
    var count = await service.DiscoverMetadataAsync(CancellationToken.None);

    // Assert
    Assert.True(count > 0);
}
```

## Monitoring

Each ingestion service should expose metrics:
- Number of entities discovered
- Polling frequency
- Error rates
- API latency

These metrics can be collected by Prometheus and visualized in Grafana.
