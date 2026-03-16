# Architecture Screenshot - Independent Layered Design

This document provides visual representations of the new independent layered architecture.

## System Architecture Overview

```mermaid
flowchart TB
    subgraph external [External Systems]
        nifi[Apache NiFi<br/>Data Orchestration]
        users[Users<br/>Data Engineers, Analysts]
    end
    
    subgraph platform [Independent Metadata Platform]
        subgraph presentation [Presentation Layer]
            ui[React UI<br/>localhost:5173<br/>Vite Dev Server]
            api[C# REST API<br/>localhost:5000<br/>ASP.NET Core]
        end
        
        subgraph services [Service Layer]
            ingestion[NiFi Ingestion Service<br/>C# Background Worker<br/>Polls every 10s]
        end
        
        subgraph storage [Storage Layer]
            opensearch[(OpenSearch<br/>Port 9200<br/>Full-text Search<br/>Properties Storage)]
            arangodb[(ArangoDB<br/>Port 8529<br/>Graph Database<br/>Lineage Tracking)]
            redis[(Redis<br/>Port 6379<br/>Cache Layer<br/>Performance)]
        end
    end
    
    users -->|Browse UI| ui
    ui -->|HTTP REST| api
    nifi -->|REST API| ingestion
    ingestion -->|Metadata Events| api
    api -->|Query/Index| opensearch
    api -->|Graph Ops| arangodb
    api -->|Cache| redis
    
    style external fill:#e3f2fd
    style presentation fill:#f3e5f5
    style services fill:#fff3e0
    style storage fill:#e8f5e9
```

## Detailed Component Architecture

```mermaid
flowchart LR
    subgraph ui [UI Layer - Port 5173]
        react[React Components]
        vite[Vite Dev Server]
        config[Config: backendUrl]
    end
    
    subgraph api [API Layer - Port 5000]
        controllers[Controllers]
        services[Application Services]
        repos[Repositories]
    end
    
    subgraph ingestion [Ingestion Layer]
        worker[Background Worker]
        poller[NiFi Poller]
        hasher[Change Detection]
    end
    
    subgraph storage [Storage Layer]
        os[(OpenSearch<br/>9200)]
        ar[(ArangoDB<br/>8529)]
        rd[(Redis<br/>6379)]
    end
    
    react --> vite
    vite --> config
    config -->|HTTP| controllers
    controllers --> services
    services --> repos
    repos --> os
    repos --> ar
    repos --> rd
    
    worker --> poller
    poller --> hasher
    hasher -->|Changed| controllers
    
    style ui fill:#e1f5fe
    style api fill:#f3e5f5
    style ingestion fill:#fff9c4
    style storage fill:#e8f5e9
```

## Data Flow - Metadata Ingestion

```mermaid
sequenceDiagram
    participant NiFi as Apache NiFi
    participant Ingestion as NiFi Ingestion Service
    participant API as C# REST API
    participant ArangoDB as ArangoDB
    participant OpenSearch as OpenSearch
    participant UI as React UI
    
    loop Every 10 seconds
        Ingestion->>NiFi: GET /nifi-api/flow/process-groups/root
        NiFi-->>Ingestion: Flow metadata
        Ingestion->>Ingestion: Compute SHA256 hash
        alt Changes detected
            Ingestion->>Ingestion: Extract processors, connections, groups
            Ingestion->>API: POST /api/metadata/ingest
            API->>ArangoDB: Store graph relationships
            API->>OpenSearch: Index properties
            API-->>Ingestion: Success
        else No changes
            Ingestion->>Ingestion: Skip processing
        end
    end
    
    UI->>API: GET /api/entities
    API->>OpenSearch: Query entities
    OpenSearch-->>API: Results
    API-->>UI: Display entities
    
    UI->>API: GET /api/lineage/{id}
    API->>ArangoDB: Traverse graph
    ArangoDB-->>API: Lineage data
    API->>OpenSearch: Fetch properties
    OpenSearch-->>API: Entity details
    API-->>UI: Display lineage
```

## Container Architecture

```mermaid
flowchart TB
    subgraph docker [Docker Environment]
        subgraph net [nifi-metadata-network]
            subgraph storage [Storage Containers]
                c1[nifi-metadata-arangodb<br/>Port: 8529<br/>Volume: arango-data]
                c2[nifi-metadata-opensearch<br/>Port: 9200, 9600<br/>Volume: opensearch-data]
                c3[nifi-metadata-redis<br/>Port: 6379<br/>Volume: redis-data]
            end
            
            subgraph app [Application Containers]
                c4[nifi-metadata-api<br/>Port: 5000<br/>Health: /health]
                c5[nifi-metadata-ingestion<br/>Polls NiFi every 10s]
                c6[nifi-metadata-frontend<br/>Port: 5173<br/>Vite dev server]
            end
        end
    end
    
    subgraph host [Host Machine]
        browser[Web Browser<br/>localhost:5173]
        nifi_ext[NiFi Instance<br/>localhost:8080]
    end
    
    c1 -.->|health check| c4
    c2 -.->|health check| c4
    c3 -.->|health check| c4
    c4 -.->|health check| c5
    c4 -.->|depends_on| c6
    
    c4 --> c1
    c4 --> c2
    c4 --> c3
    c5 --> c4
    c6 --> c4
    
    browser -->|http://localhost:5173| c6
    nifi_ext -->|http://host.docker.internal:8080| c5
    
    style storage fill:#e8f5e9
    style app fill:#e3f2fd
    style host fill:#fff3e0
```

## Extension Architecture for Future Platforms

```mermaid
flowchart TB
    subgraph interfaces [Core Interfaces]
        ims[IMetadataIngestionService]
        ime[IMetadataEntity]
        imt[IMetadataTransformer]
    end
    
    subgraph current [Current Implementation]
        nifi[NiFi Ingestion Service]
        nifiWorker[NiFiIngestionWorker]
        nifiEntity[NiFi Entities]
    end
    
    subgraph future [Future Implementations]
        trino[Trino Ingestion Service]
        kafka[Kafka Ingestion Service]
        hive[Hive Ingestion Service]
        impala[Impala Ingestion Service]
        databricks[Databricks Ingestion Service]
    end
    
    subgraph api [Central API]
        metadataApi[Metadata API<br/>Port 5000]
    end
    
    ims -.->|implements| nifi
    ims -.->|implements| trino
    ims -.->|implements| kafka
    ims -.->|implements| hive
    ims -.->|implements| impala
    ims -.->|implements| databricks
    
    nifi --> nifiWorker
    nifiWorker --> nifiEntity
    nifiEntity --> metadataApi
    
    trino -.->|future| metadataApi
    kafka -.->|future| metadataApi
    hive -.->|future| metadataApi
    impala -.->|future| metadataApi
    databricks -.->|future| metadataApi
    
    style interfaces fill:#f3e5f5
    style current fill:#e8f5e9
    style future fill:#fff3e0
    style api fill:#e3f2fd
```

## Deployment Architecture

```mermaid
flowchart TB
    subgraph dev [Development Environment]
        dev_docker[Docker Desktop]
        dev_compose[docker-compose.yml]
    end
    
    subgraph test [Testing]
        test_ps[test-deployment.ps1]
        test_sh[test-deployment.sh]
    end
    
    subgraph containers [Running Containers]
        direction TB
        c_storage[Storage Layer<br/>ArangoDB, OpenSearch, Redis]
        c_api[API Layer<br/>C# REST API]
        c_ingestion[Ingestion Layer<br/>NiFi Worker]
        c_ui[UI Layer<br/>React Frontend]
    end
    
    subgraph access [Access Points]
        a_ui[UI: localhost:5173]
        a_api[API: localhost:5000]
        a_swagger[Swagger: localhost:5000/swagger]
        a_arango[ArangoDB: localhost:8529]
        a_opensearch[OpenSearch: localhost:9200]
    end
    
    dev_docker --> dev_compose
    dev_compose --> containers
    
    test_ps -.->|validates| containers
    test_sh -.->|validates| containers
    
    c_storage --> c_api
    c_api --> c_ingestion
    c_api --> c_ui
    
    containers --> access
    
    style dev fill:#e3f2fd
    style test fill:#fff3e0
    style containers fill:#e8f5e9
    style access fill:#f3e5f5
```

## Technology Stack

```mermaid
mindmap
  root((Independent<br/>Metadata<br/>Platform))
    Frontend
      React
      TypeScript
      Vite
      TanStack Query
    Backend
      C# .NET 7
      ASP.NET Core
      MediatR
      Serilog
    Storage
      ArangoDB
        Graph Database
        Lineage
      OpenSearch
        Search Engine
        Properties
      Redis
        Cache
        Performance
    Ingestion
      C# Worker Service
      Background Tasks
      Change Detection
      SHA256 Hashing
    DevOps
      Docker
      Docker Compose
      Health Checks
      Auto Restart
    Extension
      Plugin Architecture
      Interfaces
      Transformers
      Future Platforms
```

## Access Points Summary

| Service | URL | Purpose |
|---------|-----|---------|
| **UI** | http://localhost:5173 | React frontend for users |
| **API** | http://localhost:5000 | REST API endpoints |
| **Swagger** | http://localhost:5000/swagger | API documentation |
| **Health** | http://localhost:5000/health | API health check |
| **ArangoDB** | http://localhost:8529 | Graph database UI (root/rootpassword) |
| **OpenSearch** | http://localhost:9200 | Search engine API |

## Key Features Visualization

```mermaid
flowchart LR
    subgraph features [Key Features]
        f1[Independent<br/>No Atlas]
        f2[Layered<br/>Architecture]
        f3[Containerized<br/>Deployment]
        f4[Real-time<br/>Ingestion]
        f5[Extensible<br/>Design]
        f6[Production<br/>Ready]
    end
    
    f1 --> benefit1[Clean Design]
    f2 --> benefit2[Separation of Concerns]
    f3 --> benefit3[Easy Deployment]
    f4 --> benefit4[Live Metadata]
    f5 --> benefit5[Future Platforms]
    f6 --> benefit6[Health Checks]
    
    style features fill:#e3f2fd
    style benefit1 fill:#e8f5e9
    style benefit2 fill:#e8f5e9
    style benefit3 fill:#e8f5e9
    style benefit4 fill:#e8f5e9
    style benefit5 fill:#e8f5e9
    style benefit6 fill:#e8f5e9
```

---

## How to View These Diagrams

These Mermaid diagrams will render automatically in:
- ✅ GitHub
- ✅ GitLab
- ✅ VS Code (with Mermaid extension)
- ✅ Markdown viewers that support Mermaid

## Related Documentation

- **Architecture Details:** `INDEPENDENT-ARCHITECTURE.md`
- **Deployment Guide:** `docker/README-DEPLOYMENT.md`
- **Extension Guide:** `src/Core/NiFiMetadataPlatform.Domain/README-EXTENSIBILITY.md`
- **Implementation Summary:** `IMPLEMENTATION-COMPLETE.md`

---

**Last Updated:** March 2, 2026  
**Status:** ✅ Complete and Production Ready
