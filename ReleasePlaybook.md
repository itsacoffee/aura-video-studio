# Release Playbook

This document provides step-by-step procedures for creating releases and deploying Aura Video Studio safely to production.

## Table of Contents

- [Automated Release Process](#automated-release-process)
- [Prerequisites](#prerequisites)
- [Pre-Deployment Checklist](#pre-deployment-checklist)
- [Canary Deployment Process](#canary-deployment-process)
- [Rollback Procedures](#rollback-procedures)
- [Database Migration](#database-migration)
- [Health Check Verification](#health-check-verification)
- [Post-Deployment Validation](#post-deployment-validation)
- [Troubleshooting](#troubleshooting)

## Automated Release Process

Aura Video Studio uses an automated GitHub Actions workflow to create releases with complete artifacts and release notes.

### Creating a Release

1. **Update Version Number**

   ```bash
   # Increment patch version (1.0.0 → 1.0.1)
   node scripts/release/update-version.js patch

   # Increment minor version (1.0.0 → 1.1.0)
   node scripts/release/update-version.js minor

   # Increment major version (1.0.0 → 2.0.0)
   node scripts/release/update-version.js major

   # Or set explicit version
   node scripts/release/update-version.js 1.2.3
   ```

2. **Commit Version Change**

   ```bash
   git add version.json
   git commit -m "chore: Bump version to X.Y.Z"
   git push origin main
   ```

3. **Create and Push Tag**

   ```bash
   # Create annotated tag
   git tag -a v1.2.3 -m "Release v1.2.3"
   git push origin v1.2.3
   ```

4. **Automated Workflow Execution**

   The GitHub Actions release workflow automatically:
   - Validates version format and runs security scans
   - Builds .NET backend (Release configuration)
   - Builds React frontend (production bundle)
   - Runs all unit tests (.NET + TypeScript)
   - Runs E2E tests on Windows and Linux
   - Creates portable distribution ZIP
   - Generates SHA-256 checksums
   - Generates SBOM (Software Bill of Materials)
   - Generates release notes from conventional commits
   - Creates GitHub Release with all artifacts

5. **Release Artifacts**

   The workflow produces:
   - `AuraVideoStudio_Portable_x64.zip` - Complete portable distribution
   - `AuraVideoStudio_Portable_x64.zip.sha256` - SHA-256 checksum
   - `sbom.json` - CycloneDX format SBOM
   - `attributions.txt` - Third-party license attributions
   - `RELEASE_NOTES.md` - Auto-generated from commits

### Conventional Commits

To ensure quality release notes, follow conventional commit format:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:**
- `feat`: New feature (appears in Features section)
- `fix`: Bug fix (appears in Bug Fixes section)
- `perf`: Performance improvement
- `docs`: Documentation changes
- `refactor`: Code refactoring
- `test`: Test additions/updates
- `chore`: Build/tooling changes
- `ci`: CI/CD changes

**Breaking Changes:**
Add `!` after type or include `BREAKING CHANGE:` in footer:

```
feat!: Remove deprecated API endpoint

BREAKING CHANGE: The /api/v1/old endpoint has been removed. Use /api/v2/new instead.
```

### Manual Trigger

If you need to create a release without pushing a tag:

1. Go to Actions → Release workflow
2. Click "Run workflow"
3. Enter tag version (e.g., `v1.2.3`)
4. Optionally mark as pre-release
5. Click "Run workflow"

### Version Visibility

The application version is visible:
- **API Endpoint**: `GET /api/version` returns full version info
- **UI Footer**: Version displayed in bottom-right of status bar
- **Health Check**: Version included in health endpoint responses

### Release Notes Generation

Release notes are automatically generated from commits between tags using conventional commit format. You can preview them locally:

```bash
# Generate notes for commits since last tag
node scripts/release/generate-release-notes.js v1.0.0 HEAD

# View generated RELEASE_NOTES.md
cat RELEASE_NOTES.md
```

## Prerequisites

Before initiating any deployment:

1. **All CI checks must pass**:
   - Unit tests
   - Contract tests
   - Integration tests
   - Memory regression tests
   - E2E smoke tests

2. **Required access**:
   - Production environment access
   - Database backup/restore permissions
   - Monitoring dashboards access
   - Incident management system

3. **Required tools**:
   - `kubectl` or deployment platform CLI
   - `jq` for JSON processing
   - Database client for migration verification
   - Access to log aggregation system

4. **Team availability**:
   - At least 2 engineers available during deployment
   - On-call engineer notified and standing by
   - Backup engineer identified for extended deployments

## Pre-Deployment Checklist

Complete this checklist before starting deployment:

### Code and Build

- [ ] All PRs merged to `main` branch
- [ ] CI pipeline passed on `main` branch
- [ ] Release notes prepared and reviewed
- [ ] Breaking changes documented
- [ ] Database migration scripts tested
- [ ] Release candidate artifact built and tagged

### Infrastructure

- [ ] Production environment health verified
- [ ] Database backups verified and accessible
- [ ] Sufficient disk space available (minimum 20% free)
- [ ] All dependencies updated and verified
- [ ] SSL certificates valid and not expiring soon
- [ ] CDN/static assets synced if applicable

### Monitoring

- [ ] Monitoring dashboards accessible
- [ ] Alert rules reviewed and active
- [ ] Log aggregation working
- [ ] APM/tracing configured
- [ ] Baseline metrics recorded

### Communication

- [ ] Deployment window communicated to stakeholders
- [ ] Status page updated (if applicable)
- [ ] Team chat channel created for deployment coordination
- [ ] Rollback decision maker identified

## Canary Deployment Process

Aura Video Studio uses automated canary deployment with health-check-based promotion.

### Deployment Stages

#### Stage 1: Initial Canary (5% of traffic)

```bash
# Run canary deployment script
./deploy/canary-deploy.sh production 10m
```

The script will:
1. Deploy to 5% of instances
2. Run health checks every 30 seconds for 10 minutes
3. Validate:
   - System health status (must not be "Down")
   - Provider health (warning if degraded)
   - Correlation IDs present in responses
   - Error rate within acceptable range
   - Memory usage stable
   - API response times acceptable

**Automatic rollback triggers:**
- System health endpoint returns "Down"
- 3 consecutive health check failures
- Error rate exceeds 300% of baseline
- Memory growth exceeds 500MB
- Latency P95 exceeds 2000ms

#### Stage 2: Mid-stage Canary (50% of traffic)

If Stage 1 passes:
- Deployment automatically scales to 50%
- Runs health checks for another 10 minutes
- Same validation criteria apply

#### Stage 3: Full Rollout (100% of traffic)

If Stage 2 passes:
- Deployment completes to 100%
- Final validation for 5 minutes
- Old version instances terminated

### Monitoring During Deployment

Watch these dashboards continuously:

1. **System Health Dashboard**:
   - `GET /api/health/system` - Overall system status
   - `GET /api/health/providers` - Provider health
   - `GET /api/health/ready` - Readiness check

2. **Error Rate Dashboard**:
   - HTTP 5xx errors per minute
   - Provider circuit breaker trips
   - Unhandled exceptions

3. **Performance Dashboard**:
   - Response time P50, P95, P99
   - Request throughput
   - Database query latency

4. **Resource Utilization**:
   - CPU usage per instance
   - Memory usage per instance
   - Disk I/O
   - Database connections

### Manual Canary Promotion (if automated script unavailable)

If you need to promote manually:

```bash
# 1. Deploy canary
kubectl set image deployment/aura-api api=aura-api:RC-{version} --record

# 2. Scale canary to 5%
kubectl scale deployment/aura-api-canary --replicas=1

# 3. Monitor for 10 minutes
watch -n 30 'curl -s http://api/health/system | jq'

# 4. If healthy, scale to 50%
kubectl scale deployment/aura-api-canary --replicas=5

# 5. Monitor for 10 minutes

# 6. If healthy, complete rollout
kubectl set image deployment/aura-api api=aura-api:RC-{version} --all
```

## Rollback Procedures

### Automatic Rollback

The canary deployment script automatically rolls back if health checks fail. Manual rollback may be needed if issues are detected after full rollout.

### Manual Rollback Steps

#### Immediate Rollback (within 1 hour of deployment)

```bash
# 1. Stop canary deployment if in progress
# (Ctrl+C the canary-deploy.sh script)

# 2. Scale down new version
kubectl scale deployment/aura-api --replicas=0

# 3. Scale up previous version
kubectl scale deployment/aura-api-previous --replicas=10

# 4. Verify health
curl http://api/health/system

# 5. Update DNS/load balancer to point to previous version
```

#### Rollback with Database Migration Revert

If database migrations were applied:

```bash
# 1. Stop all API instances
kubectl scale deployment/aura-api --replicas=0

# 2. Restore database snapshot
./scripts/db/restore-snapshot.sh production pre-deployment-{timestamp}

# 3. Verify database version
./scripts/db/check-version.sh

# 4. Deploy previous application version
kubectl set image deployment/aura-api api=aura-api:{previous-version}

# 5. Scale up
kubectl scale deployment/aura-api --replicas=10

# 6. Verify health
./deploy/verify-health.sh production
```

### Rollback Decision Criteria

Initiate rollback if:

- System health status becomes "Down" and doesn't recover within 5 minutes
- Error rate exceeds 300% of pre-deployment baseline
- Critical user-facing features broken
- Data corruption detected
- Security vulnerability exposed
- Memory leak causing OOM errors

## Database Migration

### Pre-Migration Steps

1. **Create database backup**:

```bash
# Automated backup with timestamp
./scripts/db/backup.sh production
```

2. **Verify backup integrity**:

```bash
# Verify backup file exists and is accessible
./scripts/db/verify-backup.sh production {backup-timestamp}
```

3. **Run migration dry-run**:

```bash
# Test migration on ephemeral database
./scripts/db/migrate.sh --dry-run --environment production
```

### Applying Migrations

Migrations are applied automatically during deployment if configured. Manual application:

```bash
# 1. Put application in maintenance mode (optional)
kubectl scale deployment/aura-api --replicas=0

# 2. Apply migrations
dotnet ef database update --project Aura.Api --context AuraDbContext

# 3. Verify migration success
./scripts/db/check-version.sh production

# 4. Bring application back up
kubectl scale deployment/aura-api --replicas=10
```

### Migration Rollback Verification

Test rollback procedure before deployment:

```bash
# 1. Apply migration to test database
dotnet ef database update --project Aura.Api --context AuraDbContext

# 2. Run rollback script
./scripts/db/rollback.sh --verify --migration {previous-migration-name}

# 3. Verify data integrity
./scripts/db/integrity-check.sh
```

## Health Check Verification

### Manual Health Check Commands

```bash
# System health (overall status)
curl -i http://api.aura.studio/api/health/system

# Provider health (individual provider status)
curl -i http://api.aura.studio/api/health/providers

# Liveness check (is application running)
curl -i http://api.aura.studio/health/live

# Readiness check (is application ready to serve traffic)
curl -i http://api.aura.studio/health/ready
```

### Expected Responses

**Healthy System**:
```json
{
  "status": "Healthy",
  "timestamp": "2025-11-02T15:00:00Z",
  "components": {
    "llm": "Healthy",
    "tts": "Healthy",
    "storage": "Healthy"
  }
}
```

**Degraded System** (acceptable, triggers warning):
```json
{
  "status": "Degraded",
  "timestamp": "2025-11-02T15:00:00Z",
  "components": {
    "llm": "Degraded",
    "tts": "Healthy",
    "storage": "Healthy"
  },
  "message": "Primary LLM provider unavailable, using fallback"
}
```

**Unhealthy System** (triggers rollback):
```json
{
  "status": "Down",
  "timestamp": "2025-11-02T15:00:00Z",
  "message": "Critical system failure"
}
```

### Correlation ID Verification

Every API response should include `X-Correlation-ID` header:

```bash
curl -I http://api.aura.studio/api/health/system | grep -i correlation
# Expected: X-Correlation-ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

Use correlation IDs to track requests across logs:

```bash
# Find all logs for a specific request
kubectl logs -l app=aura-api | grep "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

## Post-Deployment Validation

### Smoke Tests

Run automated smoke tests after deployment:

```bash
# Run E2E smoke tests against production
cd Aura.Web
PLAYWRIGHT_BASE_URL=https://aura.studio npx playwright test tests/e2e/contract-smoke.spec.ts
```

### Manual Validation Checklist

- [ ] Homepage loads successfully
- [ ] User can create new project
- [ ] Script generation works
- [ ] TTS synthesis completes
- [ ] Video rendering succeeds
- [ ] Export/download works
- [ ] Settings page accessible
- [ ] Provider health indicators accurate
- [ ] No console errors in browser

### Metrics Validation

Compare post-deployment metrics to baseline:

- Error rate should not increase by more than 10%
- P95 latency should not increase by more than 20%
- Memory usage should remain stable
- No memory leaks (memory should not grow continuously)

## Troubleshooting

### Deployment Fails at Stage 1

**Symptoms**: Health checks fail immediately after 5% canary deployment

**Actions**:
1. Check application logs for errors:
   ```bash
   kubectl logs -l app=aura-api-canary --tail=100
   ```
2. Verify configuration:
   ```bash
   kubectl get configmap aura-config -o yaml
   ```
3. Check database connectivity:
   ```bash
   kubectl exec -it aura-api-canary-pod -- curl localhost:5005/health/ready
   ```
4. Review recent code changes for obvious issues
5. Rollback and investigate offline

### High Error Rate After Deployment

**Symptoms**: Error rate >300% of baseline after deployment completes

**Actions**:
1. Identify error types from logs:
   ```bash
   kubectl logs -l app=aura-api | grep ERROR | tail -50
   ```
2. Check if errors are from specific endpoint:
   ```bash
   # Use APM or log aggregation to identify failing endpoints
   ```
3. Verify provider connections:
   ```bash
   curl http://api/api/health/providers | jq
   ```
4. If errors are widespread and critical, initiate rollback
5. If errors are isolated, consider hotfix deployment

### Memory Leak Detected

**Symptoms**: Memory usage grows continuously, doesn't stabilize

**Actions**:
1. Capture heap dump:
   ```bash
   dotnet-dump collect -p {process-id}
   ```
2. Analyze memory usage patterns
3. If memory approaches limit, initiate rollback immediately
4. Review recent changes for resource leaks (unclosed connections, event listeners, etc.)

### Database Migration Failed

**Symptoms**: Application fails to start after migration, database errors in logs

**Actions**:
1. Check migration logs for errors
2. Verify database version:
   ```bash
   ./scripts/db/check-version.sh production
   ```
3. If migration partially applied, don't retry - restore from backup:
   ```bash
   ./scripts/db/restore-snapshot.sh production pre-deployment-{timestamp}
   ```
4. Fix migration script offline
5. Test on staging environment before retrying

### Rollback Fails

**Symptoms**: Rollback script fails, both new and old versions have issues

**Actions**:
1. Stop all instances to prevent further damage
2. Restore database from most recent good backup
3. Deploy last known good version manually
4. Verify system health
5. Conduct post-incident review to prevent future occurrences

## Contact Information

### Escalation Path

1. **Primary**: On-call engineer (check current rotation)
2. **Secondary**: Backend team lead
3. **Escalation**: Engineering director

### External Dependencies

If issue relates to third-party services:

- **LLM Providers**: Check status pages for OpenAI, Anthropic, Google
- **TTS Providers**: Check ElevenLabs, PlayHT status
- **Infrastructure**: Contact cloud provider support
- **CDN**: Check CloudFlare/Fastly status

## Appendix

### Useful Commands

```bash
# View deployment status
kubectl rollout status deployment/aura-api

# View replica sets (current and previous versions)
kubectl get rs -l app=aura-api

# Get pod logs with timestamps
kubectl logs -f deployment/aura-api --timestamps

# Describe deployment for events
kubectl describe deployment aura-api

# Check resource usage
kubectl top pods -l app=aura-api
```

### Log Locations

- **Application logs**: Centralized logging system (ElasticSearch/CloudWatch)
- **Deployment logs**: CI/CD system (GitHub Actions)
- **Database logs**: Database management console
- **Infrastructure logs**: Cloud provider console

### Metrics Dashboards

- **Grafana**: https://metrics.aura.studio
- **Application Performance**: https://apm.aura.studio
- **Infrastructure**: Cloud provider console

## Document Maintenance

- **Last Updated**: 2025-11-02
- **Review Frequency**: After each production deployment
- **Owner**: DevOps Team

Update this document when:
- Deployment process changes
- New health checks added
- Rollback procedures updated
- Lessons learned from incidents
