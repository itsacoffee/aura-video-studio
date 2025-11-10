# Health Checks Runbook

## Overview

This runbook provides step-by-step procedures for diagnosing and resolving common health check failures in the Aura system.

## Quick Reference

| Health Check | Common Issues | Quick Fix |
|--------------|---------------|-----------|
| Database | Connection timeout, slow queries | Restart database, check connection string |
| Memory | High usage, memory leaks | Restart application, reduce load |
| DiskSpace | Low disk space | Clean up temp files, expand storage |
| Dependencies | FFmpeg missing, GPU not detected | Install dependencies, check drivers |
| Providers | API keys missing, rate limits | Configure API keys, check quotas |
| Startup | Application not fully initialized | Wait for initialization, check logs |

## Diagnostic Procedures

### Step 1: Check Overall Health Status

```bash
curl -s http://localhost:5005/health | jq '.'
```

Look for:
- Overall status: `healthy`, `degraded`, or `unhealthy`
- Which specific checks are failing
- Error messages in check descriptions

### Step 2: Review Individual Check Details

Get detailed information for failing checks:

```bash
# Database health
curl -s http://localhost:5005/health/db | jq '.'

# Infrastructure health (disk, memory)
curl -s http://localhost:5005/health/infrastructure | jq '.'

# Provider health
curl -s http://localhost:5005/health/providers | jq '.'
```

### Step 3: Check Application Logs

```bash
# Recent errors
tail -n 100 logs/errors-*.log

# Health check specific logs
grep "Health" logs/aura-api-*.log | tail -n 50

# Performance issues
tail -n 100 logs/performance-*.log
```

## Common Failures and Resolutions

### 1. Database Health Check Failure

#### Symptoms
```json
{
  "name": "Database",
  "status": "unhealthy",
  "description": "Cannot connect to database",
  "exception": "SqliteException: unable to open database file"
}
```

#### Diagnosis
1. Check database file exists and is accessible
2. Verify file permissions
3. Check disk space
4. Review connection string

#### Resolution

**Quick Fix:**
```bash
# Check database file
ls -la aura.db

# Fix permissions
chmod 644 aura.db

# Restart application
systemctl restart aura-api
```

**If database is corrupted:**
```bash
# Backup current database
cp aura.db aura.db.backup

# Run integrity check
sqlite3 aura.db "PRAGMA integrity_check;"

# If corrupted, restore from backup
cp /backups/aura.db.latest aura.db
systemctl restart aura-api
```

#### Prevention
- Regular database backups
- Monitor disk space
- Use WAL mode for better concurrency

---

### 2. Memory Health Check Failure

#### Symptoms
```json
{
  "name": "Memory",
  "status": "unhealthy",
  "description": "Critical memory usage: 2150 MB exceeds 2048 MB threshold",
  "data": {
    "working_set_mb": 2150,
    "gc_gen2_collections": 543
  }
}
```

#### Diagnosis
1. Check current memory usage
2. Review GC statistics
3. Look for memory leaks
4. Check for high load

#### Resolution

**Immediate Action:**
```bash
# Restart application to reclaim memory
systemctl restart aura-api

# Check system memory
free -h

# Monitor memory usage
watch -n 5 'ps aux | grep Aura.Api | grep -v grep'
```

**If memory leak suspected:**
```bash
# Enable memory profiling
export DOTNET_EnableEventLog=1
export COMPlus_GCHeapCount=4

# Capture memory dump for analysis
dotnet-dump collect -p $(pidof Aura.Api)

# Restart application
systemctl restart aura-api
```

**Configuration Adjustment:**
```json
// appsettings.json
{
  "HealthChecks": {
    "MemoryWarningThresholdMB": 1536.0,
    "MemoryCriticalThresholdMB": 2560.0
  }
}
```

#### Prevention
- Monitor memory trends
- Set up alerts for gradual memory growth
- Regular application restarts (weekly)
- Optimize video processing pipelines

---

### 3. Disk Space Health Check Failure

#### Symptoms
```json
{
  "name": "DiskSpace",
  "status": "unhealthy",
  "description": "Critical: Only 0.3 GB free on /dev/sda1. Need at least 0.5 GB.",
  "data": {
    "free_gb": 0.3,
    "total_gb": 50.0
  }
}
```

#### Diagnosis
```bash
# Check disk usage
df -h

# Find large files
du -h --max-depth=1 / | sort -hr | head -n 20

# Check temp directories
du -sh /tmp /var/tmp
```

#### Resolution

**Clean Up Temporary Files:**
```bash
# Clean temp directories
find /tmp -type f -mtime +7 -delete
find /var/tmp -type f -mtime +7 -delete

# Clean old logs
find logs/ -name "*.log" -mtime +30 -delete

# Clean rendered videos (if backed up)
find output/ -name "*.mp4" -mtime +14 -delete
```

**Expand Storage:**
```bash
# AWS EBS
aws ec2 modify-volume --volume-id vol-xxx --size 100

# Azure Disk
az disk update --name disk-name --resource-group rg --size-gb 100

# After expanding, resize filesystem
resize2fs /dev/sda1
```

**Archive Old Data:**
```bash
# Archive old projects
tar -czf projects-archive-$(date +%Y%m%d).tar.gz projects/
aws s3 cp projects-archive-*.tar.gz s3://aura-backups/
rm -rf projects/archived-*
```

#### Prevention
- Set up log rotation
- Implement automatic cleanup jobs
- Monitor disk usage trends
- Use external storage for rendered videos

---

### 4. Dependencies Health Check Failure

#### Symptoms
```json
{
  "name": "Dependencies",
  "status": "degraded",
  "description": "FFmpeg not available - video rendering disabled",
  "data": {
    "ffmpeg_available": false,
    "gpu_available": false
  }
}
```

#### Diagnosis
```bash
# Check FFmpeg installation
which ffmpeg
ffmpeg -version

# Check GPU availability
nvidia-smi  # For NVIDIA GPUs
lspci | grep VGA

# Check search paths
echo $PATH
```

#### Resolution

**Install FFmpeg:**
```bash
# Ubuntu/Debian
sudo apt update
sudo apt install ffmpeg

# RHEL/CentOS
sudo yum install epel-release
sudo yum install ffmpeg

# Windows (via Chocolatey)
choco install ffmpeg

# macOS
brew install ffmpeg
```

**Configure FFmpeg Path:**
```json
// appsettings.json
{
  "FFmpeg": {
    "ExecutablePath": "/usr/bin/ffmpeg",
    "SearchPaths": [
      "/usr/bin",
      "/usr/local/bin",
      "/opt/ffmpeg/bin"
    ]
  }
}
```

**Install GPU Drivers:**
```bash
# NVIDIA CUDA drivers
sudo apt install nvidia-driver-525
sudo reboot

# Verify installation
nvidia-smi
```

#### Prevention
- Include FFmpeg in deployment packages
- Automate dependency installation
- Document system requirements
- Test on clean systems

---

### 5. Providers Health Check Failure

#### Symptoms
```json
{
  "name": "Providers",
  "status": "degraded",
  "description": "Provider configuration has 2 warning(s)",
  "data": {
    "llm_providers_available": 0,
    "warnings": [
      "No LLM providers configured - script generation will not work",
      "No API keys configured - only offline providers will work"
    ]
  }
}
```

#### Diagnosis
1. Check provider configuration
2. Verify API keys
3. Test provider connectivity
4. Check quota/rate limits

#### Resolution

**Configure API Keys:**
```bash
# Set environment variables
export OPENAI_API_KEY="sk-..."
export ELEVENLABS_API_KEY="..."

# Or update appsettings.json
```

```json
{
  "Providers": {
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4"
    }
  }
}
```

**Test Provider Connectivity:**
```bash
# Test OpenAI
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer $OPENAI_API_KEY"

# Test from application
curl -X POST http://localhost:5005/api/providers/test \
  -H "Content-Type: application/json" \
  -d '{"provider": "OpenAI", "test_type": "connection"}'
```

**Check Quota:**
```bash
# OpenAI usage
curl https://api.openai.com/v1/usage \
  -H "Authorization: Bearer $OPENAI_API_KEY"
```

#### Prevention
- Store API keys securely (Key Vault, Secrets Manager)
- Monitor API usage and quotas
- Set up billing alerts
- Configure backup providers

---

### 6. Startup Health Check Failure

#### Symptoms
```json
{
  "name": "Startup",
  "status": "unhealthy",
  "description": "Application is still starting up",
  "data": {
    "ready": false
  }
}
```

#### Diagnosis
```bash
# Check application logs
tail -f logs/aura-api-*.log

# Check startup progress
journalctl -u aura-api -f

# Check process status
systemctl status aura-api
```

#### Resolution

**If startup is taking too long:**
1. Check for blocking operations in startup
2. Review database migrations
3. Check dependency downloads
4. Verify network connectivity

**Increase startup timeout (Kubernetes):**
```yaml
startupProbe:
  httpGet:
    path: /health/ready
    port: 5005
  initialDelaySeconds: 0
  periodSeconds: 10
  failureThreshold: 60  # 600s total
```

**Force restart if hung:**
```bash
# Graceful restart
systemctl restart aura-api

# Force kill if hung
pkill -9 -f Aura.Api
systemctl start aura-api
```

#### Prevention
- Optimize startup procedures
- Move heavy initialization to background
- Use startup probes with adequate timeout
- Monitor startup duration

---

## Emergency Procedures

### Complete System Failure

If all health checks are failing:

1. **Check system resources:**
   ```bash
   top
   df -h
   free -h
   ```

2. **Review recent changes:**
   ```bash
   git log --oneline -10
   journalctl -u aura-api --since "1 hour ago"
   ```

3. **Restart all services:**
   ```bash
   systemctl restart aura-api
   systemctl restart nginx  # if applicable
   ```

4. **Rollback if needed:**
   ```bash
   git checkout previous-stable-tag
   dotnet publish -c Release
   systemctl restart aura-api
   ```

### Database Corruption

1. **Stop application:**
   ```bash
   systemctl stop aura-api
   ```

2. **Backup current state:**
   ```bash
   cp aura.db aura.db.$(date +%Y%m%d_%H%M%S)
   ```

3. **Restore from backup:**
   ```bash
   cp /backups/aura.db.latest aura.db
   ```

4. **Verify integrity:**
   ```bash
   sqlite3 aura.db "PRAGMA integrity_check;"
   ```

5. **Restart application:**
   ```bash
   systemctl start aura-api
   ```

---

## Escalation

If issues persist after following this runbook:

1. **Gather diagnostics:**
   ```bash
   # Create diagnostic bundle
   tar -czf diagnostics-$(date +%Y%m%d).tar.gz \
     logs/ \
     aura.db \
     appsettings.json \
     /var/log/syslog
   ```

2. **Contact support:**
   - Email: support@aura.studio
   - Include: diagnostic bundle, error messages, steps taken

3. **Open incident:**
   - GitHub Issues: https://github.com/aura/aura/issues
   - Slack: #aura-support

---

## Health Check Maintenance

### Weekly Tasks
- Review health check logs
- Verify alert configurations
- Test manual failover procedures

### Monthly Tasks
- Review and adjust thresholds
- Analyze health check trends
- Update runbook with new issues
- Test disaster recovery procedures

### Quarterly Tasks
- Chaos engineering tests
- Load test health checks
- Review and optimize check performance
- Update documentation

---

## Related Documentation

- [Health Checks Guide](./HEALTH_CHECKS_GUIDE.md)
- [Monitoring Guide](./MONITORING_GUIDE.md)
- [Disaster Recovery Plan](./DISASTER_RECOVERY.md)
- [Operational Procedures](./OPERATIONAL_PROCEDURES.md)
