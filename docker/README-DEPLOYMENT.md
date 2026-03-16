# Independent Layered Architecture - Deployment Guide

This guide explains how to deploy and test the new independent layered architecture.

## Architecture Overview

```
UI Layer (Port 5173)
    ↓
API Layer (Port 5000)
    ↓
Storage Layer
    ├── OpenSearch (Port 9200) - Properties & Search
    ├── ArangoDB (Port 8529) - Graph & Relationships
    └── Redis (Port 6379) - Cache

Ingestion Layer
    └── NiFi Ingestion Service → API Layer
```

## Prerequisites

- Docker Desktop installed and running
- At least 4GB RAM available for containers
- Ports 5000, 5173, 6379, 8529, 9200 available

## Quick Start

### 1. Start All Services

```bash
cd docker
docker-compose up -d
```

This will start:
- ✅ ArangoDB (Graph database)
- ✅ OpenSearch (Search & properties)
- ✅ Redis (Cache)
- ✅ C# API (REST API)
- ✅ NiFi Ingestion Service (Background worker)
- ✅ Frontend (React UI)

### 2. Wait for Services to be Ready

Services have health checks and will start in order:
1. Storage layer (ArangoDB, OpenSearch, Redis) - ~30 seconds
2. API layer - ~10 seconds after storage is healthy
3. Ingestion service - starts after API is healthy
4. Frontend - starts after API is available

### 3. Verify Deployment

Run the test script:

**Windows (PowerShell):**
```powershell
.\test-deployment.ps1
```

**Linux/Mac:**
```bash
chmod +x test-deployment.sh
./test-deployment.sh
```

### 4. Access the Application

- **UI:** http://localhost:5173
- **API:** http://localhost:5000
- **API Swagger:** http://localhost:5000/swagger
- **API Health:** http://localhost:5000/health
- **ArangoDB UI:** http://localhost:8529 (root/rootpassword)
- **OpenSearch:** http://localhost:9200

## Manual Testing

### Test API Health

```bash
curl http://localhost:5000/health
```

Expected response: `Healthy`

### Test Storage Layer

```bash
# ArangoDB
curl http://localhost:8529/_api/version

# OpenSearch
curl http://localhost:9200/_cluster/health

# Redis (requires redis-cli)
redis-cli -h localhost -p 6379 ping
```

### Test Frontend

Open browser to http://localhost:5173 and verify:
- ✅ UI loads without errors
- ✅ Can navigate between pages
- ✅ API calls succeed (check browser console)

## Viewing Logs

### All Services
```bash
docker-compose -f docker/docker-compose.yml logs -f
```

### Specific Service
```bash
# API logs
docker-compose -f docker/docker-compose.yml logs -f csharp-api

# Ingestion service logs
docker-compose -f docker/docker-compose.yml logs -f nifi-ingestion

# Frontend logs
docker-compose -f docker/docker-compose.yml logs -f frontend
```

## Stopping Services

```bash
# Stop all services
docker-compose -f docker/docker-compose.yml down

# Stop and remove volumes (clean slate)
docker-compose -f docker/docker-compose.yml down -v
```

## Troubleshooting

### Container Won't Start

Check logs:
```bash
docker-compose -f docker/docker-compose.yml logs [container-name]
```

### Port Already in Use

Check what's using the port:
```bash
# Windows
netstat -ano | findstr :5000

# Linux/Mac
lsof -i :5000
```

### API Not Responding

1. Check if API container is running:
   ```bash
   docker ps | grep nifi-metadata-api
   ```

2. Check API logs:
   ```bash
   docker-compose -f docker/docker-compose.yml logs csharp-api
   ```

3. Verify storage layer is healthy:
   ```bash
   docker-compose -f docker/docker-compose.yml ps
   ```

### Frontend Can't Connect to API

1. Check browser console for CORS errors
2. Verify VITE_BACKEND_URL is set correctly:
   ```bash
   docker-compose -f docker/docker-compose.yml exec frontend env | grep VITE
   ```

3. Test API directly:
   ```bash
   curl http://localhost:5000/health
   ```

### NiFi Ingestion Service Issues

1. Check if NiFi is running and accessible
2. Check ingestion service logs:
   ```bash
   docker-compose -f docker/docker-compose.yml logs nifi-ingestion
   ```

3. Verify configuration:
   ```bash
   docker-compose -f docker/docker-compose.yml exec nifi-ingestion env | grep NiFi
   ```

## Development Mode

For development, you can run services individually:

### Run API Only
```bash
cd src/Presentation/NiFiMetadataPlatform.API
dotnet run
```

### Run Frontend Only
```bash
npm install
npm run dev
```

### Run Ingestion Service Only
```bash
cd src/Presentation/NiFiMetadataPlatform.NiFiIngestion
dotnet run
```

## Production Considerations

1. **Environment Variables:** Update passwords and secrets
2. **Resource Limits:** Add memory and CPU limits to docker-compose
3. **Volumes:** Use named volumes or host mounts for persistence
4. **Networking:** Use proper network isolation
5. **Monitoring:** Add Prometheus and Grafana
6. **Backup:** Implement backup strategy for ArangoDB and OpenSearch

## Architecture Benefits

✅ **Independent:** No Atlas dependencies, clean layered design  
✅ **Containerized:** All services run in Docker containers  
✅ **Extensible:** Basic extension points for future tools (Trino, Kafka, etc.)  
✅ **Real-time:** C# NiFi ingestion service for live metadata capture  
✅ **Consistent:** UI works same as before, just different backend  
✅ **Scalable:** Each layer can scale independently

## Next Steps

1. Configure NiFi connection in ingestion service
2. Test metadata ingestion from NiFi
3. Verify lineage visualization in UI
4. Add monitoring and alerting
5. Plan for additional data sources (Trino, Kafka, etc.)
