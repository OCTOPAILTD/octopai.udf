# Getting Started with NiFi Metadata Platform

This guide will help you get the NiFi Metadata Platform up and running with Docker Compose.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose v2.0 or higher
- At least 8GB of available RAM
- Ports 5000, 5173, 8529, 9200, and 6379 available

## Quick Start

### 1. Start All Services

From the `E:\Git\cloudera.udf` directory:

```bash
cd docker
docker-compose up -d
```

This will start:
- **ArangoDB** (port 8529) - Graph database for lineage
- **OpenSearch** (port 9200) - Search engine for entity metadata
- **Redis** (port 6379) - Caching layer
- **C# API** (port 5000) - REST API for assets and lineage
- **C# Worker** - Background worker for NiFi metadata ingestion
- **React Frontend** (port 5173) - Cloudera Fabric Studio UI

### 2. Verify Services are Running

Check service health:

```bash
docker-compose ps
```

All services should show as "Up".

### 3. Access the UI

Open your browser and navigate to:

```
http://localhost:5173
```

The Cloudera Fabric Studio UI should load and connect to the backend services.

### 4. Test the API

Test the C# API health endpoint:

```bash
curl http://localhost:5000/health
```

Test the Atlas-compatible search endpoint:

```bash
curl "http://localhost:5000/api/atlas/search?query=*&count=10"
```

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    React Frontend (5173)                     │
│                  Cloudera Fabric Studio UI                   │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTP
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   C# API (5000)                              │
│         Atlas-Compatible REST API                            │
│    /api/atlas/search, /lineage, /hierarchy, etc.            │
└──────────────┬─────────────────────────┬────────────────────┘
               │                         │
       ┌───────▼────────┐       ┌────────▼────────┐
       │  OpenSearch    │       │   ArangoDB      │
       │  (Port 9200)   │       │   (Port 8529)   │
       │                │       │                 │
       │ Entity Storage │       │ Graph Lineage   │
       │ & Search       │       │ Relationships   │
       └────────────────┘       └─────────────────┘
```

## Configuration

### C# API Configuration

The API is configured via environment variables in `docker-compose.yml`:

- `OpenSearch__Urls__0`: OpenSearch endpoint
- `ArangoDB__Endpoint`: ArangoDB endpoint
- `Redis__ConnectionString`: Redis connection string

### Frontend Configuration

The frontend connects to the C# API via the `VITE_BACKEND_URL` environment variable (default: `http://localhost:3001`).

## Development Workflow

### Rebuild Services

After code changes, rebuild and restart:

```bash
docker-compose down
docker-compose build
docker-compose up -d
```

### View Logs

View logs for all services:

```bash
docker-compose logs -f
```

View logs for a specific service:

```bash
docker-compose logs -f csharp-api
```

### Stop Services

Stop all services:

```bash
docker-compose down
```

Stop and remove volumes (⚠️ **Warning**: This will delete all data):

```bash
docker-compose down -v
```

## Troubleshooting

### Port Already in Use

If you see "port is already allocated" errors:

1. Check which process is using the port:
   ```bash
   netstat -ano | findstr :<PORT>
   ```

2. Stop the conflicting service or change the port in `docker-compose.yml`

### Services Not Starting

Check service logs:

```bash
docker-compose logs csharp-api
docker-compose logs opensearch
docker-compose logs arangodb
```

### Frontend Not Loading

1. Verify the frontend container is running:
   ```bash
   docker-compose ps frontend
   ```

2. Check frontend logs:
   ```bash
   docker-compose logs frontend
   ```

3. Ensure port 5173 is not blocked by firewall

### API Returning Errors

1. Verify OpenSearch and ArangoDB are healthy:
   ```bash
   curl http://localhost:9200/_cluster/health
   curl http://localhost:8529/_api/version
   ```

2. Check API logs for errors:
   ```bash
   docker-compose logs csharp-api
   ```

## Next Steps

- **Ingest NiFi Metadata**: Configure the Worker to connect to your NiFi instance
- **Explore the UI**: Browse assets, view lineage, and search metadata
- **API Documentation**: Access Swagger UI at `http://localhost:5000/swagger`

## Support

For issues or questions, please refer to the main documentation or create an issue in the repository.
