# Quick Start Guide - Independent Layered Architecture

**Get up and running in 5 minutes!** ⚡

---

## 🚀 Start the Platform

```bash
cd docker
docker-compose up -d
```

Wait ~60 seconds for all services to start and become healthy.

---

## ✅ Verify Everything Works

**Windows:**
```powershell
cd docker
.\test-deployment.ps1
```

**Linux/Mac:**
```bash
cd docker
chmod +x test-deployment.sh
./test-deployment.sh
```

Expected output: ✅ All tests passed!

---

## 🌐 Access the Application

| Service | URL | Credentials |
|---------|-----|-------------|
| **UI** | http://localhost:5173 | - |
| **API** | http://localhost:5000 | - |
| **Swagger** | http://localhost:5000/swagger | - |
| **ArangoDB** | http://localhost:8529 | root / rootpassword |
| **OpenSearch** | http://localhost:9200 | - |

---

## 📊 View Logs

```bash
# All services
docker-compose -f docker/docker-compose.yml logs -f

# Specific service
docker-compose -f docker/docker-compose.yml logs -f csharp-api
docker-compose -f docker/docker-compose.yml logs -f nifi-ingestion
docker-compose -f docker/docker-compose.yml logs -f frontend
```

---

## 🛑 Stop the Platform

```bash
cd docker
docker-compose down
```

To also remove data volumes:
```bash
docker-compose down -v
```

---

## 🏗️ Architecture at a Glance

```
┌─────────────────┐
│   React UI      │  localhost:5173
│   (Frontend)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  C# REST API    │  localhost:5000
│   (Backend)     │
└────────┬────────┘
         │
    ┌────┴────┬─────────┐
    ▼         ▼         ▼
┌────────┐ ┌────────┐ ┌────────┐
│OpenSrch│ │ArangoDB│ │ Redis  │
│  9200  │ │  8529  │ │  6379  │
└────────┘ └────────┘ └────────┘
```

---

## 🔧 Common Commands

### Check Service Health
```bash
curl http://localhost:5000/health
curl http://localhost:8529/_api/version
curl http://localhost:9200/_cluster/health
```

### Restart a Service
```bash
docker-compose -f docker/docker-compose.yml restart csharp-api
docker-compose -f docker/docker-compose.yml restart nifi-ingestion
```

### View Container Status
```bash
docker-compose -f docker/docker-compose.yml ps
```

---

## 📚 Documentation

- **Full Architecture:** `INDEPENDENT-ARCHITECTURE.md`
- **Deployment Guide:** `docker/README-DEPLOYMENT.md`
- **Extension Guide:** `src/Core/NiFiMetadataPlatform.Domain/README-EXTENSIBILITY.md`
- **Architecture Diagrams:** `ARCHITECTURE-SCREENSHOT.md`
- **Implementation Details:** `IMPLEMENTATION-COMPLETE.md`

---

## 🆘 Troubleshooting

### Services won't start?
```bash
# Check logs
docker-compose -f docker/docker-compose.yml logs

# Check Docker Desktop is running
docker ps
```

### Port already in use?
```bash
# Windows
netstat -ano | findstr :5000

# Linux/Mac
lsof -i :5000
```

### UI can't connect to API?
1. Check API is running: `curl http://localhost:5000/health`
2. Check browser console for errors
3. Verify CORS settings in API

---

## ✨ Key Features

✅ Independent (no Atlas)  
✅ Layered architecture  
✅ Containerized deployment  
✅ Real-time NiFi ingestion  
✅ Extensible for future platforms  
✅ Production ready  

---

## 🎯 Next Steps

1. ✅ Start the platform
2. ✅ Verify deployment
3. ✅ Access the UI
4. 🔄 Configure NiFi connection
5. 🔄 Test metadata ingestion
6. 🔄 Explore lineage visualization

---

**Need Help?** Check the full documentation in the files listed above.

**Status:** ✅ Ready to Use  
**Version:** 2.0  
**Last Updated:** March 2, 2026
