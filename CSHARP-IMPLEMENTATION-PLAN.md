# NiFi Metadata Platform - C# Implementation Plan

**Version:** 1.0  
**Date:** February 26, 2026  
**Technology:** C# .NET 8 with Clean Architecture

---

## Overview

Build an enterprise-grade NiFi metadata management platform in C# following Clean Architecture, DDD, and CQRS patterns. The system uses split storage (ArangoDB for graph, OpenSearch for properties) with real-time synchronization, Kubernetes deployment, and Apache Ranger authorization.

**Timeline:** 16 weeks (4 months)  
**Team:** 2-3 senior C# developers  
**Code Quality:** Enterprise-level matching Octopai standards

---

## Technology Stack

### Backend
- **Language:** C# 11 (.NET 8)
- **Framework:** ASP.NET Core 8.0
- **API:** RESTful with OpenAPI/Swagger
- **Patterns:** Clean Architecture + DDD + CQRS
- **Mediator:** MediatR for CQRS
- **Validation:** FluentValidation
- **Serialization:** System.Text.Json

### Storage
- **Graph DB:** ArangoDB 3.11+ (via ArangoDB.Client NuGet)
- **Search:** OpenSearch 2.11+ (via OpenSearch.Client NuGet)
- **Cache:** Redis 7.2+ (via StackExchange.Redis)

### Infrastructure
- **Containers:** Docker
- **Orchestration:** Kubernetes 1.28+
- **Monitoring:** Prometheus + Grafana
- **Logging:** Serilog + Seq
- **Tracing:** OpenTelemetry + Jaeger

### Testing
- **Unit Tests:** xUnit + FluentAssertions + NSubstitute
- **Integration Tests:** WebApplicationFactory + Testcontainers
- **Performance Tests:** BenchmarkDotNet
- **Architecture Tests:** NetArchTest.Rules
- **Load Tests:** NBomber or k6

---

## Project Structure

```
NiFiMetadataPlatform.sln
│
├── src/
│   ├── Core/
│   │   ├── NiFiMetadataPlatform.Domain/              # Domain entities, value objects, events
│   │   └── NiFiMetadataPlatform.Application/         # Commands, queries, handlers, DTOs
│   │
│   ├── Infrastructure/
│   │   └── NiFiMetadataPlatform.Infrastructure/      # ArangoDB, OpenSearch, Redis, Ranger
│   │
│   ├── Presentation/
│   │   ├── NiFiMetadataPlatform.API/                 # REST API controllers
│   │   └── NiFiMetadataPlatform.Worker/              # Background services
│   │
│   └── Shared/
│       └── NiFiMetadataPlatform.Contracts/           # Shared DTOs, requests, responses
│
├── tests/
│   ├── NiFiMetadataPlatform.Domain.Tests/
│   ├── NiFiMetadataPlatform.Application.Tests/
│   ├── NiFiMetadataPlatform.Integration.Tests/
│   ├── NiFiMetadataPlatform.Performance.Tests/
│   └── NiFiMetadataPlatform.Architecture.Tests/
│
├── k8s/
│   ├── base/
│   └── overlays/
│
└── docs/
```

---

## Implementation Phases

### Phase 1: Foundation & Domain Layer (Weeks 1-2)

#### 1.1 Solution Setup

**Tasks:**
- Create solution structure with Clean Architecture layers
- Setup Directory.Build.props with common properties
- Configure StyleCop/EditorConfig for code standards
- Setup CI/CD pipeline (GitHub Actions or Azure DevOps)

**Files:**
```
NiFiMetadataPlatform.sln
Directory.Build.props
Directory.Packages.props
.editorconfig
stylecop.json
.github/workflows/ci.yml
```

**Code Quality Standards:**
```xml
<!-- .editorconfig -->
root = true

[*.cs]
# Indentation
indent_style = space
indent_size = 4

# Naming conventions
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.severity = error
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interfaces_should_be_prefixed_with_i.style = begins_with_i

# Code style
csharp_prefer_braces = true:error
csharp_using_directive_placement = outside_namespace:error
csharp_prefer_simple_using_statement = true:suggestion

# Null checking
dotnet_diagnostic.CS8600.severity = error
dotnet_diagnostic.CS8602.severity = error
dotnet_diagnostic.CS8603.severity = error
```

#### 1.2 Domain Entities

**Tasks:**
- Create base `Entity<TId>` class
- Create `NiFiProcessor` entity with rich behavior
- Create `NiFiProcessGroup` entity
- Create `NiFiConnection` entity
- Add domain events for all state changes

**Key Classes:**
```csharp
// Domain/Entities/Entity.cs
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }
    private readonly List<DomainEvent> _domainEvents = new();
    
    public IReadOnlyCollection<DomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}

// Domain/Entities/NiFiProcessor.cs
public sealed class NiFiProcessor : Entity<ProcessorId>, IAggregateRoot
{
    public ProcessorFqn Fqn { get; private set; }
    public ProcessorName Name { get; private set; }
    public ProcessorType Type { get; private set; }
    public ProcessorStatus Status { get; private set; }
    // ... more properties
    
    public static NiFiProcessor Create(...) { }
    public void UpdateProperties(...) { }
    public void AddConnection(...) { }
    public void Deactivate() { }
}
```

#### 1.3 Value Objects

**Tasks:**
- Create `ProcessorFqn` value object with validation
- Create `ProcessorId` strongly-typed ID
- Create `ProcessorName` with validation
- Create `ProcessorProperties` with dictionary wrapper

**Key Classes:**
```csharp
// Domain/ValueObjects/ProcessorFqn.cs
public sealed record ProcessorFqn
{
    public string Value { get; }
    
    private ProcessorFqn(string value) => Value = value;
    
    public static ProcessorFqn Create(string containerId, string processorId)
    {
        // Validation logic
        return new ProcessorFqn($"nifi://container/{containerId}/processor/{processorId}");
    }
    
    public static ProcessorFqn Parse(string fqn)
    {
        // Parsing logic with validation
    }
}
```

#### 1.4 Domain Events

**Tasks:**
- Create base `DomainEvent` class
- Create `ProcessorCreatedEvent`
- Create `ProcessorUpdatedEvent`
- Create `ProcessorDeletedEvent`
- Create `LineageCreatedEvent`

#### 1.5 Unit Tests for Domain

**Tasks:**
- Test entity creation
- Test entity behavior methods
- Test value object validation
- Test domain events are raised
- **Target: 90%+ coverage**

**Test Example:**
```csharp
[Fact]
public void Create_WithValidParameters_ShouldCreateProcessor()
{
    var processor = NiFiProcessor.Create(fqn, name, type, parentId);
    
    processor.Should().NotBeNull();
    processor.Status.Should().Be(ProcessorStatus.Active);
    processor.GetDomainEvents().Should().ContainSingle()
        .Which.Should().BeOfType<ProcessorCreatedEvent>();
}
```

---

### Phase 2: Application Layer - CQRS (Weeks 3-4)

#### 2.1 Commands

**Tasks:**
- Create `CreateProcessorCommand` + handler + validator
- Create `UpdateProcessorCommand` + handler + validator
- Create `DeleteProcessorCommand` + handler + validator
- Create `CreateLineageCommand` + handler + validator

**Command Example:**
```csharp
// Application/Commands/CreateProcessor/CreateProcessorCommand.cs
public sealed record CreateProcessorCommand(
    string ContainerId,
    string ProcessGroupId,
    string ProcessorId,
    string Name,
    string Type,
    Dictionary<string, string> Properties) : ICommand<ProcessorDto>;

// Application/Commands/CreateProcessor/CreateProcessorCommandValidator.cs
public sealed class CreateProcessorCommandValidator 
    : AbstractValidator<CreateProcessorCommand>
{
    public CreateProcessorCommandValidator()
    {
        RuleFor(x => x.ContainerId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ProcessorId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Type).NotEmpty().Must(BeValidProcessorType);
    }
    
    private static bool BeValidProcessorType(string type)
    {
        return type.StartsWith("org.apache.nifi.processors.");
    }
}
```

#### 2.2 Queries

**Tasks:**
- Create `GetProcessorQuery` + handler
- Create `GetLineageQuery` + handler
- Create `SearchEntitiesQuery` + handler
- Create `GetHierarchyQuery` + handler

**Query Example:**
```csharp
// Application/Queries/GetLineage/GetLineageQuery.cs
public sealed record GetLineageQuery(
    string Fqn,
    int Depth,
    LineageDirection Direction) : IQuery<LineageGraphDto>;

// Application/Queries/GetLineage/GetLineageQueryHandler.cs
public sealed class GetLineageQueryHandler 
    : IQueryHandler<GetLineageQuery, LineageGraphDto>
{
    public async Task<Result<LineageGraphDto>> Handle(
        GetLineageQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Check cache
        // 2. Traverse graph in ArangoDB
        // 3. Bulk fetch from OpenSearch
        // 4. Build and return graph
    }
}
```

#### 2.3 Pipeline Behaviors

**Tasks:**
- Create `ValidationBehavior` - Validate before execution
- Create `LoggingBehavior` - Log all requests/responses
- Create `TransactionBehavior` - Wrap commands in transactions
- Create `PerformanceBehavior` - Log slow operations
- Create `CachingBehavior` - Cache query results

#### 2.4 Repository Interfaces

**Tasks:**
- Define `IGraphRepository` interface
- Define `ISearchRepository` interface
- Define `ICacheService` interface
- Define `IUnitOfWork` interface

**Interface Example:**
```csharp
// Application/Interfaces/IGraphRepository.cs
public interface IGraphRepository
{
    Task<Result> AddVertexAsync(NiFiProcessor processor, CancellationToken cancellationToken);
    Task<Result> AddEdgeAsync(string fromFqn, string toFqn, RelationshipType type, CancellationToken cancellationToken);
    Task<Result<List<string>>> TraverseLineageAsync(string fqn, int depth, LineageDirection direction, CancellationToken cancellationToken);
    Task<Result> UpdateVertexAsync(string fqn, Dictionary<string, object> updates, CancellationToken cancellationToken);
    Task<Result> DeleteVertexAsync(string fqn, CancellationToken cancellationToken);
}
```

#### 2.5 Unit Tests for Application

**Tasks:**
- Test all command handlers with mocked repositories
- Test all query handlers with mocked repositories
- Test all validators
- Test all pipeline behaviors
- **Target: 85%+ coverage**

---

### Phase 3: Infrastructure Layer - Storage (Weeks 5-6)

#### 3.1 ArangoDB Implementation

**Tasks:**
- Create `ArangoDbContext` with connection management
- Create `ArangoGraphRepository` implementing `IGraphRepository`
- Implement vertex CRUD operations (lightweight)
- Implement edge CRUD operations
- Implement graph traversal with AQL queries
- Add connection pooling and retry logic

**Key Implementation:**
```csharp
// Infrastructure/Persistence/ArangoDB/ArangoGraphRepository.cs
public sealed class ArangoGraphRepository : IGraphRepository
{
    private readonly IArangoDatabase _database;
    private readonly ILogger<ArangoGraphRepository> _logger;
    
    public async Task<Result<List<string>>> TraverseLineageAsync(
        string fqn,
        int depth,
        LineageDirection direction,
        CancellationToken cancellationToken)
    {
        var aql = $@"
            FOR v, e, p IN 1..@depth {GetDirectionString(direction)} @start relationships
                OPTIONS {{bfs: true, uniqueVertices: 'global'}}
                FILTER v.status == 'Active'
                RETURN DISTINCT v.fqn
        ";
        
        var bindVars = new Dictionary<string, object>
        {
            { "start", $"entities/{GetKeyFromFqn(fqn)}" },
            { "depth", depth }
        };
        
        var cursor = await _database.QueryAsync<string>(aql, bindVars, cancellationToken: cancellationToken);
        var fqns = await cursor.ToListAsync(cancellationToken);
        
        return Result.Success(fqns);
    }
}
```

#### 3.2 OpenSearch Implementation

**Tasks:**
- Create `OpenSearchContext` with client configuration
- Create `OpenSearchRepository` implementing `ISearchRepository`
- Implement entity indexing (complete properties)
- Implement bulk fetch operations
- Implement full-text search
- Implement faceted search
- Add retry logic and circuit breaker

**Key Implementation:**
```csharp
// Infrastructure/Persistence/OpenSearch/OpenSearchRepository.cs
public sealed class OpenSearchRepository : ISearchRepository
{
    private readonly IOpenSearchClient _client;
    
    public async Task<Result<List<NiFiProcessor>>> BulkGetAsync(
        List<string> fqns,
        CancellationToken cancellationToken)
    {
        var response = await _client.MultiGetAsync(
            m => m.Index("nifi_entities").GetMany<ProcessorDocument>(fqns),
            cancellationToken);
        
        var processors = response.Hits
            .Where(h => h.Found)
            .Select(h => h.Source.ToDomainEntity())
            .ToList();
        
        return Result.Success(processors);
    }
    
    public async Task<Result<SearchResults>> SearchAsync(
        string query,
        SearchFilters filters,
        CancellationToken cancellationToken)
    {
        var searchResponse = await _client.SearchAsync<ProcessorDocument>(
            s => s.Index("nifi_entities")
                  .Query(q => BuildQuery(query, filters))
                  .Size(filters.Limit)
                  .From(filters.Offset),
            cancellationToken);
        
        return Result.Success(MapToSearchResults(searchResponse));
    }
}
```

#### 3.3 Redis Caching

**Tasks:**
- Create `RedisCacheService` implementing `ICacheService`
- Create `MemoryCacheService` for L1 cache
- Create `HybridCacheService` combining both
- Implement cache invalidation strategies

**Key Implementation:**
```csharp
// Infrastructure/Caching/HybridCacheService.cs
public sealed class HybridCacheService : ICacheService
{
    private readonly IMemoryCache _l1Cache;
    private readonly IDistributedCache _l2Cache;
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        // Try L1 (memory) first
        if (_l1Cache.TryGetValue(key, out T? value))
            return value;
        
        // Try L2 (Redis) second
        var bytes = await _l2Cache.GetAsync(key, cancellationToken);
        if (bytes != null)
        {
            value = JsonSerializer.Deserialize<T>(bytes);
            _l1Cache.Set(key, value, TimeSpan.FromMinutes(1));
            return value;
        }
        
        return default;
    }
}
```

#### 3.4 Unit of Work

**Tasks:**
- Create `UnitOfWork` for transaction management
- Implement two-phase commit for dual writes
- Implement rollback logic
- Add transaction logging

#### 3.5 Integration Tests

**Tasks:**
- Test ArangoDB operations with Testcontainers
- Test OpenSearch operations with Testcontainers
- Test dual-write consistency
- Test rollback scenarios
- **Target: 80%+ coverage**

---

### Phase 4: Real-Time Ingestion (Weeks 7-8)

#### 4.1 NiFi API Client

**Tasks:**
- Create `INiFiApiClient` interface
- Implement `NiFiApiClient` with HttpClient
- Implement recursive process group fetching
- Implement processor details fetching
- Add authentication (username/password or token)
- Add retry logic with Polly

**Key Implementation:**
```csharp
// Infrastructure/Ingestion/NiFiApiClient.cs
public sealed class NiFiApiClient : INiFiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NiFiApiClient> _logger;
    
    public async Task<List<NiFiProcessGroupDto>> GetProcessGroupRecursiveAsync(
        string processGroupId,
        CancellationToken cancellationToken)
    {
        var url = $"/nifi-api/flow/process-groups/{processGroupId}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var data = await response.Content.ReadFromJsonAsync<ProcessGroupResponse>(cancellationToken);
        
        // Recursively fetch child process groups
        var children = new List<NiFiProcessGroupDto>();
        foreach (var childId in data.ProcessGroup.ChildProcessGroupIds)
        {
            var childGroups = await GetProcessGroupRecursiveAsync(childId, cancellationToken);
            children.AddRange(childGroups);
        }
        
        return children;
    }
}
```

#### 4.2 Change Detection

**Tasks:**
- Create `IChangeDetector` interface
- Implement hash-based change detection
- Store hashes in Redis
- Detect new, updated, deleted entities

**Key Implementation:**
```csharp
// Infrastructure/Ingestion/ChangeDetector.cs
public sealed class ChangeDetector : IChangeDetector
{
    public async Task<ChangeSet> DetectChangesAsync(
        List<NiFiProcessorDto> currentState,
        CancellationToken cancellationToken)
    {
        var currentHashes = currentState.ToDictionary(
            p => p.Id,
            p => ComputeHash(p));
        
        var previousHashes = await LoadPreviousHashesAsync(cancellationToken);
        
        var newProcessors = currentHashes.Keys.Except(previousHashes.Keys).ToList();
        var deletedProcessors = previousHashes.Keys.Except(currentHashes.Keys).ToList();
        var updatedProcessors = currentHashes
            .Where(kvp => previousHashes.ContainsKey(kvp.Key) && 
                         previousHashes[kvp.Key] != kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();
        
        return new ChangeSet(newProcessors, updatedProcessors, deletedProcessors);
    }
    
    private static string ComputeHash(NiFiProcessorDto processor)
    {
        var json = JsonSerializer.Serialize(processor);
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }
}
```

#### 4.3 Background Monitor Service

**Tasks:**
- Create `NiFiMonitorBackgroundService` inheriting `BackgroundService`
- Implement polling loop (every 10 seconds)
- Call change detector
- Send commands via MediatR for detected changes
- Add error handling and retry logic

#### 4.4 Metadata Parser

**Tasks:**
- Create SQL parser for ExecuteSQL processors
- Extract column lineage using GSP library (or similar)
- Extract database connection information
- Build lineage edges

**Key Implementation:**
```csharp
// Infrastructure/Ingestion/MetadataParser.cs
public sealed class MetadataParser : IMetadataParser
{
    public async Task<ProcessorMetadata> ParseAsync(
        NiFiProcessorDto processor,
        CancellationToken cancellationToken)
    {
        var metadata = new ProcessorMetadata
        {
            ProcessorId = processor.Id,
            ProcessorType = processor.Type
        };
        
        // Extract SQL if ExecuteSQL processor
        if (processor.Type.Contains("ExecuteSQL"))
        {
            var sql = processor.Config.Properties.GetValueOrDefault("SQL select query");
            if (!string.IsNullOrEmpty(sql))
            {
                metadata.SqlQuery = sql;
                metadata.ColumnLineage = await ParseSqlLineageAsync(sql, cancellationToken);
            }
        }
        
        return metadata;
    }
    
    private async Task<List<ColumnLineage>> ParseSqlLineageAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        // Use GSP or similar SQL parser
        // Extract source and target columns
        // Return lineage mappings
        throw new NotImplementedException();
    }
}
```

#### 4.5 Integration Tests

**Tasks:**
- Test NiFi API client with mock server
- Test change detection algorithm
- Test end-to-end ingestion flow
- Test SQL parsing and lineage extraction

---

### Phase 5: API Layer (Weeks 9-10)

#### 5.1 Controllers

**Tasks:**
- Create `EntitiesController` with CRUD endpoints
- Create `LineageController` with lineage endpoints
- Create `SearchController` with search endpoints
- Create `HealthController` with health checks
- Add XML documentation comments
- Generate OpenAPI specification

#### 5.2 Middleware

**Tasks:**
- Create `ExceptionHandlingMiddleware` - Global error handling
- Create `RequestLoggingMiddleware` - Log all requests
- Create `AuthenticationMiddleware` - JWT validation
- Create `AuthorizationMiddleware` - Ranger integration
- Create `MetricsMiddleware` - Prometheus metrics

**Middleware Example:**
```csharp
// API/Middleware/ExceptionHandlingMiddleware.cs
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Validation failed",
                Errors = validationEx.Errors.Select(e => e.ErrorMessage).ToList()
            },
            NotFoundException notFoundEx => new ErrorResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = notFoundEx.Message
            },
            UnauthorizedException => new ErrorResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Unauthorized"
            },
            _ => new ErrorResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "An internal server error occurred"
            }
        };
        
        context.Response.StatusCode = response.Status;
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

#### 5.3 API Tests

**Tasks:**
- Test all controller endpoints
- Test authentication/authorization
- Test error handling
- Test OpenAPI specification
- **Target: 85%+ coverage**

---

### Phase 6: Security & Ranger Integration (Weeks 11-12)

#### 6.1 Ranger Client

**Tasks:**
- Create `IRangerClient` interface
- Implement `RangerClient` with HttpClient
- Implement `CheckAccess` method
- Add retry logic and circuit breaker

**Key Implementation:**
```csharp
// Infrastructure/Security/RangerClient.cs
public sealed class RangerClient : IRangerClient
{
    private readonly HttpClient _httpClient;
    
    public async Task<RangerAccessResult> CheckAccessAsync(
        RangerAccessRequest request,
        CancellationToken cancellationToken)
    {
        var url = "/service/public/v2/api/access/check";
        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<RangerAccessResult>(cancellationToken);
        return result!;
    }
}
```

#### 6.2 Authorization Service

**Tasks:**
- Create `RangerAuthorizationService` implementing `IAuthorizationService`
- Implement permission checking with caching
- Implement policy cache with Redis
- Add graceful degradation when Ranger unavailable

#### 6.3 Authorization Attribute

**Tasks:**
- Create `RangerAuthorizationAttribute` for controllers
- Implement authorization filter
- Extract resource/action from request
- Check permission via authorization service

**Key Implementation:**
```csharp
// API/Filters/RangerAuthorizationAttribute.cs
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RangerAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var authService = context.HttpContext.RequestServices
            .GetRequiredService<IAuthorizationService>();
        
        var user = context.HttpContext.User.Identity?.Name 
            ?? throw new UnauthorizedException();
        
        var resourceFqn = ExtractResourceFqn(context);
        
        var allowed = await authService.CheckPermissionAsync(
            user,
            Resource,
            resourceFqn,
            Action);
        
        if (!allowed)
        {
            context.Result = new ForbidResult();
        }
    }
}
```

#### 6.4 Security Tests

**Tasks:**
- Test Ranger client
- Test authorization service
- Test authorization attribute
- Test policy caching
- Test graceful degradation

---

### Phase 7: Kubernetes Deployment (Weeks 13-14)

#### 7.1 Docker Images

**Tasks:**
- Create multi-stage Dockerfile for API
- Create Dockerfile for Worker
- Optimize image size (< 200 MB)
- Add health check support

**Dockerfile Example:**
```dockerfile
# Dockerfile.api
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY NiFiMetadataPlatform.sln ./
COPY src/Core/NiFiMetadataPlatform.Domain/*.csproj ./src/Core/NiFiMetadataPlatform.Domain/
COPY src/Core/NiFiMetadataPlatform.Application/*.csproj ./src/Core/NiFiMetadataPlatform.Application/
COPY src/Infrastructure/NiFiMetadataPlatform.Infrastructure/*.csproj ./src/Infrastructure/NiFiMetadataPlatform.Infrastructure/
COPY src/Presentation/NiFiMetadataPlatform.API/*.csproj ./src/Presentation/NiFiMetadataPlatform.API/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ ./src/

# Build
WORKDIR /src/src/Presentation/NiFiMetadataPlatform.API
RUN dotnet build -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8000/health || exit 1

EXPOSE 8000
ENTRYPOINT ["dotnet", "NiFiMetadataPlatform.API.dll"]
```

#### 7.2 Kubernetes Manifests

**Tasks:**
- Create Deployment for API (3 replicas)
- Create Deployment for Worker (1 replica)
- Create StatefulSet for ArangoDB (3 replicas)
- Create StatefulSet for OpenSearch (3 replicas)
- Create StatefulSet for Redis (3 replicas)
- Create Services for all components
- Create Ingress for external access
- Create ConfigMaps for configuration
- Create Secrets for credentials
- Create HPA for API and Worker

**Files:**
```
k8s/base/
├── api-deployment.yaml
├── worker-deployment.yaml
├── arangodb-statefulset.yaml
├── opensearch-statefulset.yaml
├── redis-statefulset.yaml
├── services.yaml
├── ingress.yaml
├── configmaps.yaml
├── secrets.yaml
└── hpa.yaml
```

#### 7.3 Kustomize Overlays

**Tasks:**
- Create dev overlay (minimal resources)
- Create staging overlay (medium resources)
- Create production overlay (full resources)

**Structure:**
```
k8s/
├── base/
└── overlays/
    ├── dev/
    │   └── kustomization.yaml
    ├── staging/
    │   └── kustomization.yaml
    └── production/
        └── kustomization.yaml
```

#### 7.4 Deployment Scripts

**Tasks:**
- Create deployment script for K8s
- Create rollback script
- Create database initialization script
- Create backup script

---

### Phase 8: Monitoring & Observability (Weeks 15-16)

#### 8.1 Prometheus Metrics

**Tasks:**
- Add prometheus-net package
- Create custom metrics (request count, duration, cache hits)
- Expose /metrics endpoint
- Add business metrics (entity counts, lineage depth)

**Key Implementation:**
```csharp
// Infrastructure/Monitoring/PrometheusMetrics.cs
public static class PrometheusMetrics
{
    public static readonly Counter RequestCount = Metrics.CreateCounter(
        "api_requests_total",
        "Total API requests",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "endpoint", "status" }
        });
    
    public static readonly Histogram RequestDuration = Metrics.CreateHistogram(
        "api_request_duration_seconds",
        "API request duration",
        new HistogramConfiguration
        {
            LabelNames = new[] { "method", "endpoint" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
        });
    
    public static readonly Gauge EntityCount = Metrics.CreateGauge(
        "entities_total",
        "Total entities",
        new GaugeConfiguration
        {
            LabelNames = new[] { "type", "platform" }
        });
}
```

#### 8.2 Grafana Dashboards

**Tasks:**
- Create System Health dashboard
- Create Performance dashboard
- Create Business Metrics dashboard
- Create Storage Metrics dashboard

**Dashboard JSON:**
```json
{
  "dashboard": {
    "title": "NiFi Metadata Platform - System Health",
    "panels": [
      {
        "title": "Request Rate",
        "targets": [
          {
            "expr": "rate(api_requests_total[5m])"
          }
        ]
      },
      {
        "title": "P95 Latency",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(api_request_duration_seconds_bucket[5m]))"
          }
        ]
      }
    ]
  }
}
```

#### 8.3 Structured Logging

**Tasks:**
- Configure Serilog with structured logging
- Add request correlation IDs
- Log to console (K8s logs)
- Log to Seq for centralized logging
- Add log enrichers (machine name, environment)

**Configuration:**
```csharp
// API/Program.cs
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console(new JsonFormatter())
        .WriteTo.Seq(context.Configuration["Seq:Url"]!);
});
```

#### 8.4 Distributed Tracing

**Tasks:**
- Add OpenTelemetry package
- Configure tracing for HTTP requests
- Configure tracing for database calls
- Export to Jaeger

#### 8.5 Alert Rules

**Tasks:**
- Create Prometheus alert rules
- Configure AlertManager
- Setup Slack/email notifications

**Alert Rules:**
```yaml
groups:
- name: nifi_metadata_alerts
  rules:
  - alert: HighErrorRate
    expr: rate(api_requests_total{status=~"5.."}[5m]) > 0.05
    for: 5m
    labels:
      severity: critical
    annotations:
      summary: "High error rate detected"
  
  - alert: SlowLineageQueries
    expr: histogram_quantile(0.95, rate(api_request_duration_seconds_bucket{endpoint="/lineage"}[5m])) > 1.0
    for: 10m
    labels:
      severity: warning
    annotations:
      summary: "Slow lineage queries"
```

---

## Testing Strategy

### Test Pyramid

```
E2E Tests (10%)
  - User scenarios
  - Performance tests
  - Load tests
  ↑
Integration Tests (20%)
  - API tests with real databases
  - End-to-end flows
  - Testcontainers
  ↑
Unit Tests (70%)
  - Domain entities
  - Value objects
  - Command/query handlers
  - Services
  - Mocked dependencies
```

### Coverage Targets

| Layer | Target | Tools |
|-------|--------|-------|
| Domain | 90%+ | xUnit, FluentAssertions |
| Application | 85%+ | xUnit, NSubstitute |
| Infrastructure | 70%+ | xUnit, Testcontainers |
| API | 80%+ | WebApplicationFactory |
| Overall | 80%+ | Coverlet, ReportGenerator |

### Test Execution

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Run performance tests
dotnet run --project tests/NiFiMetadataPlatform.Performance.Tests -c Release

# Run load tests
dotnet run --project tests/NiFiMetadataPlatform.Load.Tests -- --users 100 --duration 300

# Run architecture tests
dotnet test tests/NiFiMetadataPlatform.Architecture.Tests
```

---

## Code Quality Standards

### Naming Conventions

```csharp
// ✅ GOOD: Descriptive names
public sealed class CreateProcessorCommandHandler { }
public interface IGraphRepository { }
public sealed record ProcessorFqn { }

// ❌ BAD: Abbreviations and unclear names
public class CrtProcCmdHdlr { }
public interface IRepo { }
public record ProcFQN { }
```

### Error Handling

```csharp
// ✅ GOOD: Result pattern
public async Task<Result<Processor>> GetProcessorAsync(string fqn)
{
    try
    {
        var processor = await _repository.GetAsync(fqn);
        return Result.Success(processor);
    }
    catch (NotFoundException ex)
    {
        return Result.Failure<Processor>(ex.Message);
    }
}

// ❌ BAD: Throwing exceptions for control flow
public async Task<Processor> GetProcessorAsync(string fqn)
{
    var processor = await _repository.GetAsync(fqn);
    if (processor == null)
        throw new NotFoundException(); // Don't use exceptions for expected cases
    return processor;
}
```

### Async Best Practices

```csharp
// ✅ GOOD: Async all the way
public async Task<Result> ProcessAsync(CancellationToken cancellationToken)
{
    var data = await FetchDataAsync(cancellationToken).ConfigureAwait(false);
    return await SaveDataAsync(data, cancellationToken).ConfigureAwait(false);
}

// ❌ BAD: Sync over async
public Result Process()
{
    var data = FetchDataAsync(CancellationToken.None).Result; // DEADLOCK RISK
    return SaveDataAsync(data, CancellationToken.None).Result;
}
```

### Immutability

```csharp
// ✅ GOOD: Immutable value objects with records
public sealed record ProcessorFqn(string Value);

// ✅ GOOD: Immutable collections
public IReadOnlyCollection<Tag> Tags { get; }

// ❌ BAD: Mutable value objects
public class ProcessorFqn
{
    public string Value { get; set; }
}
```

---

## Performance Targets

| Operation | Target | Measurement |
|-----------|--------|-------------|
| Entity fetch | < 50ms | BenchmarkDotNet |
| Search (100 results) | < 100ms | BenchmarkDotNet |
| 1-hop lineage | < 100ms | BenchmarkDotNet |
| 5-hop lineage | < 200ms | BenchmarkDotNet |
| 10-hop lineage | < 500ms | BenchmarkDotNet |
| Entity create | < 200ms | BenchmarkDotNet |
| Concurrent users | 100+ | NBomber/k6 |
| Requests/second | 1000+ | NBomber/k6 |

---

## Deliverables

### Code Deliverables

- [ ] Complete C# solution with Clean Architecture
- [ ] Domain layer with rich entities and value objects
- [ ] Application layer with CQRS commands/queries
- [ ] Infrastructure layer with ArangoDB + OpenSearch + Redis
- [ ] API layer with REST endpoints
- [ ] Worker service for real-time ingestion
- [ ] 80%+ test coverage
- [ ] Architecture tests passing

### Documentation Deliverables

- [ ] Architecture documentation (this file)
- [ ] API specification (OpenAPI)
- [ ] Deployment guide
- [ ] Operations runbook
- [ ] Performance tuning guide
- [ ] Ranger integration guide

### Deployment Deliverables

- [ ] Docker images for API and Worker
- [ ] Kubernetes manifests (Deployments, StatefulSets, Services)
- [ ] Helm chart
- [ ] Monitoring dashboards
- [ ] Alert rules
- [ ] Backup scripts

---

## Success Criteria

### MVP (Weeks 1-10)

- [ ] Real-time NiFi metadata capture (< 30s latency)
- [ ] Entity CRUD operations
- [ ] Lineage visualization (5-hop) < 200ms
- [ ] Search with filters
- [ ] 80%+ test coverage
- [ ] Deployed to local K8s

### Production (Weeks 1-16)

- [ ] 99.9% uptime
- [ ] < 100ms P95 latency for entity fetch
- [ ] < 500ms P95 latency for 10-hop lineage
- [ ] 1000+ req/sec throughput
- [ ] 1M+ entities supported
- [ ] Full Ranger integration
- [ ] Complete monitoring
- [ ] Disaster recovery tested

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| C# learning curve | Low | Medium | Team has C# experience |
| ArangoDB C# client limitations | Medium | Medium | Fallback to REST API |
| Consistency issues | Medium | High | Two-phase commit + reconciliation |
| Performance targets | Medium | High | Early performance testing |
| Ranger integration complexity | Medium | Medium | Phased implementation with fallback |

---

## Next Steps

1. **Approve Architecture** - Review and approve this plan
2. **Setup Development Environment** - Install .NET 8, Docker, K8s
3. **Create Solution Structure** - Setup projects and dependencies
4. **Begin Phase 1** - Implement Domain layer
5. **Iterate Weekly** - Review progress, adjust plan
6. **Deploy to Staging** - Validate in staging environment
7. **Production Rollout** - Gradual rollout with monitoring

---

**Estimated Effort:** 640-800 hours (2-3 developers × 4 months)  
**Infrastructure Cost:** $670-$3650/month depending on scale  
**Total Project Cost:** $120,000-$187,500 (including development)
