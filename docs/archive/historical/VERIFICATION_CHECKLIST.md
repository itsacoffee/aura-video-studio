# PR #1 Verification Checklist

Use this checklist to verify the local development environment setup works correctly.

## Pre-Verification Setup

- [ ] Clean clone of repository OR `make clean` to start fresh
- [ ] Docker Desktop is running
- [ ] No services running on ports 3000, 5005, or 6379

## Installation Verification

### 1. Setup Script

**Linux/macOS:**
```bash
./scripts/setup-local.sh
```

**Windows:**
```powershell
.\scripts\setup-local.ps1
```

**Expected Output:**
- [ ] ✓ Docker version detected
- [ ] ✓ Docker Compose detected
- [ ] ✓ All directories created (data, logs, temp-media)
- [ ] ✓ .env created from .env.example
- [ ] ✓ Ports 5005, 3000, 6379 reported as available
- [ ] ✓ Docker images pulled
- [ ] ✓ Helper scripts created

**Time:** Should complete in 1-3 minutes

### 2. Service Startup

```bash
make dev
```

**Expected Output:**
- [ ] Redis container starts
- [ ] FFmpeg container starts  
- [ ] API container starts and becomes healthy
- [ ] Web container starts and becomes healthy
- [ ] No error messages in logs
- [ ] All services show "healthy" status

**Time:** Should complete in 45-60 seconds

## Health Check Verification

### 3. API Health

```bash
curl http://localhost:5005/health/live
```

**Expected:**
- [ ] HTTP 200 response
- [ ] JSON response with "status": "Healthy"

```bash
make health
```

**Expected:**
- [ ] ✓ API is healthy
- [ ] ✓ Redis is healthy
- [ ] ✓ Web is accessible

### 4. Service Status

```bash
make status
```

**Expected:**
- [ ] aura_api_1 - State: Up, Status: healthy
- [ ] aura_web_1 - State: Up, Status: healthy
- [ ] aura_redis_1 - State: Up, Status: healthy
- [ ] aura_ffmpeg_1 - State: Up

## Functionality Verification

### 5. Web UI Access

**Open browser to http://localhost:3000**

**Expected:**
- [ ] UI loads without errors
- [ ] No console errors in browser dev tools
- [ ] Can navigate between pages
- [ ] API requests succeed (check Network tab)

### 6. API Swagger Documentation

**Open browser to http://localhost:5005/swagger**

**Expected:**
- [ ] Swagger UI loads
- [ ] API endpoints listed
- [ ] Can expand and view endpoint details

### 7. Database Verification

```bash
# Check database file exists
ls -la data/aura.db

# Check sample data (requires sqlite3)
sqlite3 data/aura.db "SELECT COUNT(*) FROM Projects;"
```

**Expected:**
- [ ] aura.db file exists in data/ directory
- [ ] Sample data returns count of 3 projects
- [ ] No database errors

**Alternative (via API):**
```bash
curl http://localhost:5005/api/v1/projects
```

**Expected:**
- [ ] Returns JSON array
- [ ] Contains sample projects

### 8. Logs Verification

```bash
make logs | head -20
```

**Expected:**
- [ ] Logs from all services visible
- [ ] Timestamps present
- [ ] No ERROR level messages
- [ ] Structured log format

**Check log files:**
```bash
ls -la logs/
```

**Expected:**
- [ ] Log files created in logs/ directory

## Command Verification

### 9. Management Commands

```bash
# Stop services
make stop
```
**Expected:** All services stop gracefully

```bash
# Restart services
make restart
```
**Expected:** All services restart and become healthy again

```bash
# View individual logs
make logs-api | head -10
make logs-web | head -10
```
**Expected:** Service-specific logs displayed

### 10. Database Commands

```bash
# Run migrations (should be no-op after initial setup)
make db-migrate
```
**Expected:** Migrations reported as up-to-date

## Negative Testing

### 11. Port Conflict Detection

```bash
# Start a service on port 5005 in another terminal
nc -l 5005

# In main terminal
make dev
```

**Expected:**
- [ ] Port conflict detected
- [ ] Clear error message
- [ ] Instructions to resolve

### 12. Missing Environment Variable

```bash
# Remove a variable from .env
# Try starting services
make dev
```

**Expected:**
- [ ] Services start with defaults OR
- [ ] Clear error message about missing config

## VS Code Integration

### 13. Launch Configurations

**Open VS Code and:**
- [ ] Open Command Palette (Ctrl+Shift+P)
- [ ] Type "Debug: Select and Start Debugging"
- [ ] See "Full Stack (Docker)" configuration
- [ ] Can start debugging session

### 14. Tasks

**In VS Code:**
- [ ] Terminal → Run Task (Ctrl+Shift+P → "Tasks: Run Task")
- [ ] See all custom tasks (start-all-services, stop-all-services, etc.)
- [ ] Can execute tasks successfully

## Documentation Verification

### 15. Documentation Completeness

- [ ] README.md has updated quick start section
- [ ] DEVELOPMENT.md exists and is comprehensive
- [ ] TROUBLESHOOTING.md exists with solutions
- [ ] QUICK_REFERENCE.md provides one-page reference
- [ ] All documentation renders correctly in Markdown

### 16. Help System

```bash
make help
```

**Expected:**
- [ ] Clear list of all available commands
- [ ] Descriptions for each command
- [ ] Quick start instructions
- [ ] Colored output for readability

## Performance Verification

### 17. Resource Usage

```bash
docker stats --no-stream
```

**Expected:**
- [ ] Total CPU usage < 50% (on 4+ core system)
- [ ] Total memory usage < 2GB
- [ ] No services using excessive resources

### 18. Startup Time

**Measure from `make dev` to all healthy:**

**Expected:**
- [ ] First run (with build): 2-3 minutes
- [ ] Subsequent runs: 45-60 seconds
- [ ] All services healthy within timeout

## Cleanup Verification

### 19. Clean Shutdown

```bash
make stop
```

**Expected:**
- [ ] All services stop gracefully
- [ ] No orphaned containers: `docker ps -a`
- [ ] Data persists after stop

### 20. Full Cleanup

```bash
make clean
```

**Expected:**
- [ ] All containers removed
- [ ] All volumes removed (if specified)
- [ ] Network removed
- [ ] Can restart cleanly with `make dev`

## Cross-Platform Verification

### 21. Platform-Specific

**Windows:**
- [ ] PowerShell scripts execute without errors
- [ ] Paths work with backslashes
- [ ] Docker Desktop integration works

**macOS:**
- [ ] File watching works for hot reload
- [ ] No performance issues with Docker
- [ ] M1/M2 compatibility (if applicable)

**Linux:**
- [ ] File permissions correct
- [ ] Docker socket accessible
- [ ] No SELinux issues

## Final Verification

### 22. End-to-End Developer Experience

**Simulate a new developer:**
1. [ ] Clone repository
2. [ ] Run setup script
3. [ ] Execute `make dev`
4. [ ] Open browser to http://localhost:3000
5. [ ] Make a code change in Aura.Web/src
6. [ ] Verify hot reload works
7. [ ] Make a code change in Aura.Api
8. [ ] Rebuild with `docker-compose up --build -d api`
9. [ ] View logs with `make logs`
10. [ ] Stop services with `make stop`

**Total time for new developer:** < 10 minutes

### 23. Troubleshooting Guide Test

**Pick 3 issues from TROUBLESHOOTING.md:**
- [ ] Follow solutions for each
- [ ] Verify solutions work
- [ ] Verify commands are correct

## Acceptance Criteria Review

- [ ] ✅ Single command brings up all services
- [ ] ✅ All services report healthy within 60 seconds
- [ ] ✅ Frontend can make authenticated API calls
- [ ] ✅ Database has test data loaded
- [ ] ✅ Logs aggregated and visible via make logs
- [ ] ✅ Health checks for all dependencies
- [ ] ✅ Structured logging to ./logs/ directory
- [ ] ✅ Port collision detection and reporting
- [ ] ✅ Service startup order enforcement
- [ ] ✅ Secrets only in .env files (gitignored)
- [ ] ✅ Default passwords documented as insecure
- [ ] ✅ CORS properly configured for local dev
- [ ] ✅ Idempotent migration scripts
- [ ] ✅ Rollback script for each migration
- [ ] ✅ Migration version tracking table

## Sign-off

**Verified by:** _________________

**Date:** _________________

**Platform:** [ ] Windows [ ] macOS [ ] Linux

**Docker version:** _________________

**Issues found:** _________________

**Notes:**
```
[Space for additional notes]
```

---

## Quick Verification (5 minutes)

For a fast sanity check:

```bash
# 1. Setup
./scripts/setup-local.sh

# 2. Start
make dev-detached

# 3. Wait 60 seconds, then check
sleep 60 && make health

# 4. Open browser
open http://localhost:3000  # macOS
xdg-open http://localhost:3000  # Linux
start http://localhost:3000  # Windows

# 5. Check logs
make logs | tail -50

# 6. Cleanup
make stop
```

**Expected:** All green checkmarks, no errors.
