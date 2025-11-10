# Deployment Runbook

## Overview

This runbook covers deploying Aura Video Studio to production and staging environments.

**Severity**: N/A (Scheduled Maintenance)  
**Estimated Time**: 30-60 minutes  
**Prerequisites**: Admin access, approved change request

## Pre-Deployment Checklist

- [ ] All tests passing in CI/CD
- [ ] Code reviewed and approved
- [ ] Change request approved
- [ ] Stakeholders notified of maintenance window
- [ ] Backup completed
- [ ] Rollback plan documented
- [ ] Database migrations reviewed
- [ ] Dependencies updated and tested

## Deployment Types

### 1. Standard Deployment (Zero Downtime)

For minor updates and feature releases.

### 2. Maintenance Window Deployment

For breaking changes, database migrations, or major updates.

### 3. Hotfix Deployment

For critical bug fixes requiring immediate deployment.

## Standard Deployment Procedure

### Step 1: Pre-Deployment Verification

```bash
# Verify current production status
curl http://localhost:5005/api/v1/health

# Check current version
curl http://localhost:5005/api/v1/capabilities | jq '.version'

# Verify database connection
sqlite3 aura.db "SELECT COUNT(*) FROM Projects;"

# Check disk space
df -h

# Check memory
free -h
```

### Step 2: Backup

```bash
# Backup database
sqlite3 aura.db ".backup 'backup/aura-$(date +%Y%m%d-%H%M%S).db'"

# Backup configuration
cp appsettings.json backup/appsettings-$(date +%Y%m%d-%H%M%S).json

# Backup user data
tar -czf backup/userdata-$(date +%Y%m%d-%H%M%S).tar.gz \
  secrets.dat \
  logs/ \
  output/
```

### Step 3: Stop Services

```bash
# Stop API service
systemctl stop aura-api

# Or for Docker deployment
docker-compose down

# Verify services stopped
ps aux | grep -E "(dotnet|Aura)"
```

### Step 4: Deploy New Version

#### For Native Deployment

```bash
# Pull latest code
git fetch origin
git checkout v1.2.3  # Replace with target version

# Restore dependencies
dotnet restore

# Build solution
dotnet build --configuration Release

# Run database migrations
dotnet ef database update --project Aura.Api

# Verify build output
ls -la Aura.Api/bin/Release/net8.0/
```

#### For Docker Deployment

```bash
# Pull latest images
docker-compose pull

# Or build locally
docker-compose build --no-cache

# Verify images
docker images | grep aura
```

### Step 5: Update Configuration

```bash
# Review configuration changes
diff backup/appsettings-*.json appsettings.json

# Update environment-specific settings
nano appsettings.Production.json

# Verify secrets are encrypted
ls -la secrets.dat
```

### Step 6: Start Services

```bash
# Start API service
systemctl start aura-api

# Or for Docker
docker-compose up -d

# Wait for startup (30 seconds)
sleep 30
```

### Step 7: Verify Deployment

```bash
# Check health endpoint
curl http://localhost:5005/api/v1/health

# Expected output:
# {
#   "status": "healthy",
#   "version": "1.2.3",
#   "timestamp": "2024-11-10T10:30:00Z"
# }

# Check logs for errors
tail -n 100 logs/aura-api-*.log | grep -i error

# Test critical endpoints
curl -X POST http://localhost:5005/api/v1/health/ready

# Check database connectivity
curl http://localhost:5005/api/v1/diagnostics/database

# Verify provider status
curl http://localhost:5005/api/v1/providers/status
```

### Step 8: Smoke Tests

```bash
# Run automated smoke tests
cd Aura.E2E
dotnet test --filter "Category=Smoke"

# Or use test script
./scripts/smoke/start_and_probe.sh
```

### Step 9: Monitor

Monitor for 30 minutes after deployment:

```bash
# Watch logs in real-time
tail -f logs/aura-api-*.log

# Monitor error rate
watch -n 5 'grep -c ERROR logs/aura-api-*.log'

# Monitor memory usage
watch -n 5 'free -h'

# Monitor disk space
watch -n 5 'df -h'
```

### Step 10: Post-Deployment Tasks

- [ ] Update deployment log
- [ ] Notify stakeholders of successful deployment
- [ ] Monitor error rates for 24 hours
- [ ] Update documentation if needed
- [ ] Close change request

## Rollback Procedure

If deployment fails or causes issues:

### Step 1: Immediate Rollback

```bash
# Stop current services
systemctl stop aura-api

# Or for Docker
docker-compose down
```

### Step 2: Restore Previous Version

#### For Native Deployment

```bash
# Checkout previous version
git checkout v1.2.2  # Previous stable version

# Rebuild
dotnet build --configuration Release

# Restore database from backup if needed
sqlite3 aura.db ".restore 'backup/aura-YYYYMMDD-HHMMSS.db'"
```

#### For Docker Deployment

```bash
# Use previous image version
docker-compose down
docker-compose -f docker-compose.v1.2.2.yml up -d
```

### Step 3: Verify Rollback

```bash
# Check health
curl http://localhost:5005/api/v1/health

# Verify version
curl http://localhost:5005/api/v1/capabilities | jq '.version'

# Run smoke tests
./scripts/smoke/start_and_probe.sh
```

### Step 4: Root Cause Analysis

- Collect logs from failed deployment
- Review error messages
- Identify root cause
- Create incident report
- Update deployment procedures

## Database Migrations

### Safe Migration Procedure

```bash
# Generate migration script
dotnet ef migrations script --output migration.sql

# Review SQL script
cat migration.sql

# Test on staging first
sqlite3 staging.db < migration.sql

# Backup production database
sqlite3 aura.db ".backup 'backup/pre-migration-$(date +%Y%m%d-%H%M%S).db'"

# Apply migration
dotnet ef database update --project Aura.Api

# Verify migration
dotnet ef migrations list --project Aura.Api
```

### Rollback Migration

```bash
# Revert to previous migration
dotnet ef database update PreviousMigrationName --project Aura.Api

# Or restore from backup
sqlite3 aura.db ".restore 'backup/pre-migration-YYYYMMDD-HHMMSS.db'"
```

## Troubleshooting

### Issue: Health Check Fails After Deployment

**Symptoms**: `/api/v1/health` returns 503 or times out

**Diagnosis**:
```bash
# Check if service is running
systemctl status aura-api

# Check logs for startup errors
tail -n 100 logs/aura-api-*.log

# Check database connectivity
sqlite3 aura.db "SELECT 1;"
```

**Resolution**:
1. Check for configuration errors in `appsettings.json`
2. Verify database file permissions
3. Ensure all dependencies are restored
4. Check port conflicts (5005)

### Issue: Database Migration Fails

**Symptoms**: Migration command returns error

**Diagnosis**:
```bash
# Check current migration state
dotnet ef migrations list --project Aura.Api

# Check database schema
sqlite3 aura.db ".schema"
```

**Resolution**:
1. Review migration script for errors
2. Check for conflicting migrations
3. Restore from backup if corrupted
4. Apply migrations incrementally

### Issue: High Memory Usage After Deployment

**Symptoms**: Memory usage increases significantly

**Diagnosis**:
```bash
# Check memory usage
free -h

# Check process memory
ps aux | grep dotnet | awk '{print $4, $11}'

# Check for memory leaks
dotnet-counters monitor --process-id $(pgrep -f Aura.Api)
```

**Resolution**:
1. Restart service to clear memory
2. Check for memory leaks in new code
3. Adjust GC settings if needed
4. Consider rollback if persistent

## Best Practices

### Before Deployment

- Always test in staging first
- Review all code changes
- Check for breaking changes
- Verify database migrations
- Complete backup before deploying

### During Deployment

- Follow runbook exactly
- Document any deviations
- Communicate status updates
- Monitor key metrics
- Be prepared to rollback

### After Deployment

- Monitor for 24 hours
- Check error logs regularly
- Verify user reports
- Update documentation
- Conduct post-mortem if issues

## Automation

### CI/CD Pipeline

```yaml
# .github/workflows/deploy.yml
name: Deploy to Production

on:
  push:
    tags:
      - 'v*'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Build and test
        run: |
          dotnet restore
          dotnet build --configuration Release
          dotnet test
      - name: Deploy
        run: |
          ./scripts/deploy/deploy-production.sh
      - name: Verify
        run: |
          ./scripts/smoke/start_and_probe.sh
```

### Deployment Script

```bash
#!/bin/bash
# scripts/deploy/deploy-production.sh

set -e

echo "Starting deployment..."

# Pre-deployment checks
./scripts/deploy/pre-deploy-checks.sh

# Backup
./scripts/backup/backup-production.sh

# Deploy
./scripts/deploy/deploy.sh

# Verify
./scripts/smoke/start_and_probe.sh

echo "Deployment complete!"
```

## References

- [Backup and Restore Runbook](./backup-restore.md)
- [Monitoring Runbook](./monitoring.md)
- [Database Maintenance](./database-maintenance.md)
- [Troubleshooting Guide](../../TROUBLESHOOTING.md)

---

**Last Updated**: 2024-11-10  
**Version**: 1.0
