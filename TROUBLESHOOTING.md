# Aura Video Studio - Troubleshooting Guide

This guide covers common issues and solutions for local development of Aura Video Studio.

## Table of Contents

- [Quick Diagnostics](#quick-diagnostics)
- [Docker Issues](#docker-issues)
- [API Issues](#api-issues)
- [Frontend Issues](#frontend-issues)
- [Database Issues](#database-issues)
- [Network and Connectivity](#network-and-connectivity)
- [Performance Issues](#performance-issues)
- [Platform-Specific Issues](#platform-specific-issues)
- [Getting Help](#getting-help)

## Quick Diagnostics

### Health Check

Run this first to identify issues:

```bash
# Check all services
make health

# Check individual service status
make status

# View recent logs
make logs | tail -n 100
```

### Common Quick Fixes

```bash
# 1. Restart all services
make restart

# 2. Clean restart (preserves data)
make stop && make dev

# 3. Nuclear option (deletes all data)
make clean && make dev
```

### Diagnostic Commands

```bash
# Check Docker is running
docker ps

# Check port availability
./scripts/setup/check-ports.sh

# Validate configuration
./scripts/setup/validate-config.sh

# Check disk space
df -h

# Check system resources
docker stats
```

## Docker Issues

### Issue: "Cannot connect to Docker daemon"

**Symptoms:**
```
Cannot connect to the Docker daemon at unix:///var/run/docker.sock
```

**Solutions:**

1. **Start Docker Desktop:**
   - Windows/macOS: Open Docker Desktop application
   - Linux: `sudo systemctl start docker`

2. **Check Docker is running:**
   ```bash
   docker ps
   ```

3. **Verify permissions (Linux):**
   ```bash
   sudo usermod -aG docker $USER
   # Log out and log back in
   ```

### Issue: "Port is already allocated"

**Symptoms:**
```
Error: Bind for 0.0.0.0:5005 failed: port is already allocated
```

**Solutions:**

1. **Find process using the port:**
   ```bash
   # Linux/macOS
   lsof -i :5005
   
   # Windows
   netstat -ano | findstr :5005
   ```

2. **Kill the process:**
   ```bash
   # Linux/macOS
   kill -9 <PID>
   
   # Windows
   taskkill /PID <PID> /F
   ```

3. **Or change ports in `docker-compose.yml`:**
   ```yaml
   services:
     api:
       ports:
         - "5006:5005"  # Use different host port
   ```

### Issue: "No space left on device"

**Symptoms:**
```
ERROR: no space left on device
```

**Solutions:**

1. **Clean up Docker:**
   ```bash
   docker system prune -af
   docker volume prune -f
   ```

2. **Remove unused images:**
   ```bash
   docker image prune -a
   ```

3. **Check disk space:**
   ```bash
   df -h
   ```

### Issue: Slow Docker builds

**Solutions:**

1. **Enable BuildKit:**
   ```bash
   export DOCKER_BUILDKIT=1
   docker-compose build
   ```

2. **Use Docker layer caching:**
   ```bash
   docker-compose build --parallel
   ```

3. **Increase Docker resources:**
   - Docker Desktop → Settings → Resources
   - Increase CPU, Memory, and Disk limits

## API Issues

### Issue: API container exits immediately

**Symptoms:**
```
api_1  | exited with code 139
```

**Solutions:**

1. **Check logs:**
   ```bash
   make logs-api
   ```

2. **Common causes and fixes:**

   **Missing database directory:**
   ```bash
   mkdir -p data logs temp-media
   ```

   **Permission issues:**
   ```bash
   chmod -R 755 data logs temp-media
   ```

   **Configuration error:**
   ```bash
   # Validate appsettings.Development.json is valid JSON
   cat Aura.Api/appsettings.Development.json | python -m json.tool
   ```

### Issue: API returns 503 Service Unavailable

**Symptoms:**
- Health check fails
- All API requests return 503

**Solutions:**

1. **Check API is starting:**
   ```bash
   docker-compose logs -f api
   ```

2. **Wait for dependencies:**
   - API depends on Redis
   - Check Redis is healthy: `docker-compose ps redis`

3. **Check startup health:**
   ```bash
   curl http://localhost:5005/health/startup
   ```

### Issue: "FFmpeg not found"

**Symptoms:**
```
FFmpeg not available - video rendering disabled
```

**Solutions:**

1. **Check FFmpeg in container:**
   ```bash
   docker-compose exec api which ffmpeg
   docker-compose exec api ffmpeg -version
   ```

2. **Rebuild API container:**
   ```bash
   docker-compose build --no-cache api
   docker-compose up -d api
   ```

3. **Use local FFmpeg:**
   ```bash
   # Update .env
   AURA_FFMPEG_PATH=/usr/local/bin/ffmpeg
   ```

### Issue: Database migration errors

**Symptoms:**
```
error MSB4018: The "GetManifestFilePath" task failed unexpectedly
```

**Solutions:**

1. **Run migrations manually:**
   ```bash
   cd Aura.Api
   dotnet ef database update --verbose
   ```

2. **Check database file permissions:**
   ```bash
   ls -la data/aura.db
   chmod 644 data/aura.db
   ```

3. **Reset database:**
   ```bash
   make db-reset
   ```

## Frontend Issues

### Issue: Web container exits with code 1

**Symptoms:**
```
web_1  | npm ERR! code ELIFECYCLE
```

**Solutions:**

1. **Check logs:**
   ```bash
   make logs-web
   ```

2. **Common causes:**

   **Dependency issues:**
   ```bash
   cd Aura.Web
   rm -rf node_modules package-lock.json
   npm ci
   ```

   **Vite cache corruption:**
   ```bash
   rm -rf Aura.Web/node_modules/.vite
   ```

   **Port conflict:**
   - Change port in `docker-compose.yml`

### Issue: "Cannot connect to API"

**Symptoms:**
- Frontend shows connection errors
- API requests fail with CORS errors

**Solutions:**

1. **Check API is running:**
   ```bash
   curl http://localhost:5005/health/live
   ```

2. **Wait for API to start:**
   - API takes ~30-60 seconds to fully start
   - Check: `make health`

3. **Verify CORS configuration:**
   - Check `appsettings.Development.json` includes frontend URL
   - Ensure `.env.development` has correct `VITE_API_BASE_URL`

4. **Check proxy configuration:**
   ```typescript
   // Aura.Web/vite.config.ts
   server: {
     proxy: {
       '/api': {
         target: 'http://127.0.0.1:5005',
         changeOrigin: true,
       },
     },
   }
   ```

### Issue: Hot reload not working

**Solutions:**

1. **Restart web container:**
   ```bash
   docker-compose restart web
   ```

2. **Check file watching:**
   - Docker Desktop → Settings → Resources → File Sharing
   - Ensure workspace directory is shared

3. **Use polling (slower but more reliable):**
   ```bash
   # Add to package.json scripts
   "dev": "vite --force --poll"
   ```

## Database Issues

### Issue: "Database is locked"

**Symptoms:**
```
SQLite Error: database is locked
```

**Solutions:**

1. **Close other connections:**
   ```bash
   # Stop all services
   make stop
   
   # Remove lock files
   rm -f data/aura.db-shm data/aura.db-wal
   
   # Restart
   make dev
   ```

2. **Use WAL mode (should be default):**
   ```sql
   PRAGMA journal_mode=WAL;
   ```

### Issue: "Database disk image is malformed"

**Symptoms:**
```
SQLite error: database disk image is malformed
```

**Solutions:**

1. **Backup and recover:**
   ```bash
   # Backup current database
   cp data/aura.db data/aura.db.backup
   
   # Try to recover
   sqlite3 data/aura.db ".recover" | sqlite3 data/aura.db.recovered
   mv data/aura.db.recovered data/aura.db
   ```

2. **Reset database (loses data):**
   ```bash
   make db-reset
   ```

### Issue: Missing tables or schema errors

**Solutions:**

1. **Run migrations:**
   ```bash
   make db-migrate
   ```

2. **Check migration status:**
   ```bash
   docker-compose exec api dotnet ef migrations list
   ```

3. **Force migration:**
   ```bash
   cd Aura.Api
   dotnet ef database update --force
   ```

## Network and Connectivity

### Issue: "Name resolution failed"

**Symptoms:**
```
Name or service not known: redis
```

**Solutions:**

1. **Check Docker network:**
   ```bash
   docker network ls
   docker network inspect aura-network
   ```

2. **Recreate network:**
   ```bash
   make clean
   make dev
   ```

3. **Use IP instead of hostname:**
   ```bash
   # Find Redis IP
   docker inspect aura_redis_1 | grep IPAddress
   
   # Update .env
   AURA_REDIS_CONNECTION=172.x.x.x:6379
   ```

### Issue: CORS errors in browser

**Symptoms:**
```
Access to XMLHttpRequest blocked by CORS policy
```

**Solutions:**

1. **Check allowed origins in API:**
   ```json
   // appsettings.Development.json
   "Cors": {
     "AllowedOrigins": [
       "http://localhost:3000",
       "http://localhost:5173"
     ]
   }
   ```

2. **Clear browser cache:**
   - Hard refresh: Ctrl+F5 (Windows) or Cmd+Shift+R (Mac)

3. **Check browser console:**
   - Verify request URL matches `VITE_API_BASE_URL`

## Performance Issues

### Issue: Slow container startup

**Solutions:**

1. **Use volume mounts for dependencies:**
   - Already configured in `docker-compose.yml`

2. **Increase Docker resources:**
   - Docker Desktop → Settings → Resources
   - Recommended: 4+ CPUs, 8GB+ RAM

3. **Disable BuildKit (sometimes faster):**
   ```bash
   DOCKER_BUILDKIT=0 docker-compose build
   ```

### Issue: High CPU usage

**Solutions:**

1. **Check which service is causing it:**
   ```bash
   docker stats
   ```

2. **Common causes:**
   - **API:** Large video rendering jobs
   - **Web:** Hot reload watching too many files
   - **Redis:** Large cache size

3. **Mitigations:**
   ```bash
   # Limit concurrent renders in .env
   AURA_MAX_CONCURRENT_RENDERS=1
   
   # Reduce file watching in web
   # Use --poll with longer interval
   ```

### Issue: High memory usage

**Solutions:**

1. **Check memory usage:**
   ```bash
   docker stats --no-stream
   ```

2. **Increase Docker memory limit:**
   - Docker Desktop → Settings → Resources

3. **Clear caches:**
   ```bash
   # Clear Redis cache
   docker-compose exec redis redis-cli FLUSHALL
   
   # Clear Vite cache
   rm -rf Aura.Web/node_modules/.vite
   ```

## Platform-Specific Issues

### Windows

#### WSL 2 Issues

**Issue: Slow file I/O**

**Solution:**
- Move project inside WSL filesystem
- Use WSL 2 terminal, not PowerShell

**Issue: Docker Desktop not starting**

**Solution:**
1. Enable WSL 2: `wsl --set-default-version 2`
2. Update WSL: `wsl --update`
3. Restart Docker Desktop

#### PowerShell Script Execution

**Issue: "execution of scripts is disabled"**

**Solution:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### macOS

#### M1/M2 Silicon Issues

**Issue: Platform compatibility warnings**

**Solution:**
- Use multi-platform images (already configured)
- Rebuild with: `docker-compose build --no-cache`

**Issue: Rosetta errors**

**Solution:**
- Docker Desktop → Settings → General
- Enable "Use Rosetta for x86/amd64 emulation"

### Linux

#### Permission Issues

**Issue: Permission denied errors**

**Solution:**
```bash
# Add user to docker group
sudo usermod -aG docker $USER

# Fix file permissions
sudo chown -R $USER:$USER data logs temp-media
chmod -R 755 data logs temp-media
```

#### SELinux Issues

**Issue: Permission denied with SELinux enabled**

**Solution:**
```bash
# Add SELinux context
chcon -Rt svirt_sandbox_file_t data logs temp-media

# Or disable SELinux (not recommended)
sudo setenforce 0
```

## Getting Help

### Before Asking for Help

1. **Check logs:**
   ```bash
   make logs > debug.log
   ```

2. **Run diagnostics:**
   ```bash
   make health
   make status
   docker ps -a
   docker compose config
   ```

3. **Try clean restart:**
   ```bash
   make clean && make dev
   ```

### Reporting Issues

When reporting issues, include:

1. **Environment info:**
   - OS and version
   - Docker version: `docker --version`
   - Docker Compose version: `docker-compose --version`

2. **Error messages:**
   - Full error text
   - Relevant log excerpts

3. **Steps to reproduce:**
   - Exact commands run
   - Configuration changes made

4. **Output of:**
   ```bash
   make status
   make health
   docker ps -a
   ```

### Where to Get Help

- **GitHub Issues:** Bug reports and feature requests
- **GitHub Discussions:** Questions and community support
- **Documentation:** `docs/troubleshooting/` for detailed guides
- **Logs:** Always check logs first: `make logs`

---

**Still stuck?** Open an issue with the diagnostic information above, and we'll help! 🤝
