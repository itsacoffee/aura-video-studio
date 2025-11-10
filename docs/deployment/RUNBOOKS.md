# Deployment Runbooks

## Overview

This document contains step-by-step procedures for common deployment scenarios and incident response.

## Table of Contents

1. [Standard Production Deployment](#standard-production-deployment)
2. [Emergency Hotfix Deployment](#emergency-hotfix-deployment)
3. [Rollback Procedure](#rollback-procedure)
4. [Database Migration](#database-migration)
5. [Scaling Operations](#scaling-operations)
6. [Incident Response](#incident-response)

---

## Standard Production Deployment

### Prerequisites

- [ ] All tests passing in CI
- [ ] Code review approved
- [ ] Security scan passed
- [ ] Staging deployment successful
- [ ] Change management ticket approved
- [ ] Deployment window scheduled

### Procedure

#### 1. Pre-deployment (T-30 minutes)

```bash
# Set deployment variables
export VERSION="v1.2.3"
export ENVIRONMENT="production"
export DEPLOYMENT_STRATEGY="blue-green"

# Verify prerequisites
./deploy/audit-deployment.sh $ENVIRONMENT $VERSION $DEPLOYMENT_STRATEGY initiated log
```

**Notifications**:
```bash
# Post in Slack
/msg #deployments Starting production deployment v1.2.3 at $(date)

# Email stakeholders
cat <<EOF | mail -s "Production Deployment Starting" stakeholders@aura.studio
Production deployment of v1.2.3 is starting.

Expected duration: 15 minutes
Deployment strategy: Blue-green
Expected completion: $(date -d '+30 minutes')

Monitor at: https://github.com/aura/aura/actions
EOF
```

#### 2. Backup (T-20 minutes)

```bash
# Backup production database
docker exec aura-production-api \
  sqlite3 /app/data/aura.db ".backup /app/data/aura.db.backup"

# Verify backup
docker exec aura-production-api \
  sqlite3 /app/data/aura.db.backup "SELECT COUNT(*) FROM videos"

# Backup configuration
cp data/aura.db data/aura.db.backup.$(date +%Y%m%d_%H%M%S)
```

#### 3. Deployment (T-0)

```bash
# Trigger deployment via GitHub Actions
gh workflow run production-deploy.yml \
  --field environment=production \
  --field version=$VERSION \
  --field deployment_strategy=$DEPLOYMENT_STRATEGY

# Or manual deployment
./deploy/blue-green-deploy.sh $ENVIRONMENT $VERSION
```

**Monitor Progress**:
```bash
# Watch workflow
gh run watch

# Monitor logs
gh run view --log

# Check deployment status
watch -n 10 ./deploy/validate-deployment.sh $ENVIRONMENT
```

#### 4. Validation (T+15 minutes)

```bash
# Run comprehensive validation
export DEPLOY_VERSION=$VERSION
./deploy/validate-deployment.sh production https://api.aura.studio

# Check key metrics
curl https://api.aura.studio/api/diagnostics/metrics | jq '.'

# Verify version
curl https://api.aura.studio/api/version | jq '.version'
```

#### 5. Post-deployment (T+20 minutes)

```bash
# Update audit log
./deploy/audit-deployment.sh $ENVIRONMENT $VERSION $DEPLOYMENT_STRATEGY success update

# Cleanup old containers
docker system prune -f

# Send success notification
cat <<EOF | mail -s "Production Deployment Successful" stakeholders@aura.studio
Production deployment of v1.2.3 completed successfully.

Deployment duration: 15 minutes
Version deployed: v1.2.3
Health check status: All systems operational

Application URL: https://aura.studio
Monitoring: https://monitoring.aura.studio
EOF
```

### Verification Steps

- [ ] API health check returns 200
- [ ] Web frontend loads successfully
- [ ] Database connectivity confirmed
- [ ] Redis connectivity confirmed
- [ ] All critical features working
- [ ] No error spikes in logs
- [ ] Response times within SLA
- [ ] Version number correct

### Success Criteria

✅ Deployment successful if:
- All health checks passing
- No critical errors in logs
- Response time < 2000ms (p95)
- Error rate < 1%
- Zero downtime achieved

---

## Emergency Hotfix Deployment

### When to Use

- Critical security vulnerability
- Data loss bug
- Complete service outage
- Critical business impact

### Procedure

#### 1. Incident Declaration (T-0)

```bash
# Declare incident
INCIDENT_ID="INC-$(date +%Y%m%d-%H%M%S)"
echo "Incident $INCIDENT_ID declared at $(date)"

# Notify on-call team
# (Use PagerDuty, Slack, or configured alerting)
```

#### 2. Fix Development (T+5 minutes)

```bash
# Create hotfix branch
git checkout main
git pull origin main
git checkout -b hotfix/critical-issue-$INCIDENT_ID

# Make fix
# ... code changes ...

# Commit
git add .
git commit -m "fix(critical): resolve data loss in video processing

Closes $INCIDENT_ID"
```

#### 3. Fast-track Testing (T+10 minutes)

```bash
# Run focused tests
dotnet test --filter "FullyQualifiedName~CriticalTests"

# Quick E2E test
npm run test:e2e:critical

# Deploy to staging
./deploy/blue-green-deploy.sh staging hotfix-$INCIDENT_ID
```

#### 4. Emergency Deployment (T+15 minutes)

```bash
# Merge and tag
git checkout main
git merge hotfix/critical-issue-$INCIDENT_ID --no-ff
git tag v1.2.4-hotfix
git push origin main v1.2.4-hotfix

# Fast deployment (skip non-critical checks)
gh workflow run production-deploy.yml \
  --field environment=production \
  --field version=v1.2.4-hotfix \
  --field skip_tests=true \
  --field deployment_strategy=rolling
```

#### 5. Verification (T+20 minutes)

```bash
# Verify fix
./deploy/validate-deployment.sh production

# Monitor for 30 minutes
watch -n 60 'curl -s https://api.aura.studio/api/health/system | jq .'
```

#### 6. Post-incident (T+1 hour)

```bash
# Document incident
cat > incidents/$INCIDENT_ID.md <<EOF
# Incident Report: $INCIDENT_ID

## Summary
Critical issue requiring emergency hotfix deployment

## Timeline
- T+0: Incident detected
- T+5: Hotfix development started
- T+15: Emergency deployment initiated
- T+20: Fix verified

## Root Cause
[Description]

## Resolution
Deployed v1.2.4-hotfix

## Preventive Measures
- [ ] Add monitoring for similar issues
- [ ] Improve testing coverage
- [ ] Update runbooks
EOF

# Conduct post-mortem
# Schedule within 48 hours
```

---

## Rollback Procedure

### When to Rollback

Immediate rollback if:
- Health checks failing for 3+ minutes
- Error rate > 10%
- Response time > 10 seconds
- Critical feature broken
- Data corruption detected

### Quick Rollback (Blue-Green)

```bash
# Execute rollback
./deploy/rollback.sh production "Rollback due to [REASON]"

# This automatically:
# 1. Switches traffic back to previous environment
# 2. Verifies rollback
# 3. Logs rollback action
# 4. Sends notifications
```

### Manual Rollback

If automated rollback fails:

```bash
# 1. Identify previous version
PREVIOUS_VERSION=$(git describe --tags --abbrev=0 HEAD^)

# 2. Stop current deployment
docker compose -p aura-production down

# 3. Deploy previous version
docker compose -p aura-production \
  -f docker-compose.prod.yml \
  up -d \
  --build \
  -e VERSION=$PREVIOUS_VERSION

# 4. Wait for services
sleep 60

# 5. Verify rollback
./deploy/validate-deployment.sh production
```

### Database Rollback

If database migration needs rollback:

```bash
# 1. Stop API
docker compose -p aura-production stop api

# 2. Restore database
cp data/aura.db.backup data/aura.db

# Or rollback migration
docker exec aura-production-api \
  dotnet ef database update [PREVIOUS_MIGRATION]

# 3. Restart API
docker compose -p aura-production start api

# 4. Verify
curl https://api.aura.studio/api/health/system
```

### Post-rollback

```bash
# Document rollback
./deploy/audit-deployment.sh production $VERSION rollback success

# Create incident ticket
gh issue create \
  --title "Production rollback - v$VERSION" \
  --body "Rollback executed due to [REASON]"

# Notify team
# Send notification via configured channels
```

---

## Database Migration

### Pre-migration

```bash
# 1. Review migration
dotnet ef migrations script > migration.sql
cat migration.sql  # Review changes

# 2. Backup database
docker exec aura-production-api \
  sqlite3 /app/data/aura.db ".backup /app/data/aura.db.backup"

# 3. Test migration in staging
./deploy/blue-green-deploy.sh staging v1.2.3
```

### Migration Deployment

```bash
# 1. Enable maintenance mode
curl -X POST https://api.aura.studio/api/admin/maintenance \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"enabled": true, "message": "Database maintenance in progress"}'

# 2. Deploy with migration
./deploy/blue-green-deploy.sh production v1.2.3

# Migration runs automatically on startup

# 3. Verify migration
curl https://api.aura.studio/api/health/system | jq '.database'

# 4. Disable maintenance mode
curl -X POST https://api.aura.studio/api/admin/maintenance \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"enabled": false}'
```

### Rollback Migration

```bash
# 1. Enable maintenance mode
curl -X POST https://api.aura.studio/api/admin/maintenance \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"enabled": true}'

# 2. Rollback to previous migration
docker exec aura-production-api \
  dotnet ef database update [PREVIOUS_MIGRATION]

# 3. Verify
docker exec aura-production-api \
  dotnet ef migrations list

# 4. Disable maintenance mode
curl -X POST https://api.aura.studio/api/admin/maintenance \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"enabled": false}'
```

---

## Scaling Operations

### Scale Up API Instances

```bash
# Increase replicas
docker compose -p aura-production \
  up -d --scale api=4

# Or with Kubernetes
kubectl scale deployment aura-api --replicas=4

# Verify
docker ps | grep aura-api
# or
kubectl get pods -l app=aura-api
```

### Scale Down API Instances

```bash
# Decrease replicas
docker compose -p aura-production \
  up -d --scale api=2

# Or with Kubernetes
kubectl scale deployment aura-api --replicas=2
```

### Auto-scaling Configuration

```bash
# Configure horizontal pod autoscaler (Kubernetes)
kubectl autoscale deployment aura-api \
  --min=2 \
  --max=10 \
  --cpu-percent=70

# Verify autoscaler
kubectl get hpa
```

---

## Incident Response

### Severity Levels

**SEV1 - Critical**: Complete outage, data loss
**SEV2 - High**: Major feature broken, performance degraded
**SEV3 - Medium**: Minor feature issue, workaround available
**SEV4 - Low**: Cosmetic issue, no user impact

### SEV1 Response

```bash
# 1. Declare incident
echo "SEV1: $(date)"

# 2. Assess situation
curl https://api.aura.studio/health
docker ps
docker logs aura-production-api --tail 100

# 3. Immediate mitigation
# Option A: Rollback
./deploy/rollback.sh production "SEV1 incident"

# Option B: Scale up
docker compose up -d --scale api=6

# Option C: Restart services
docker compose restart

# 4. Monitor recovery
watch -n 10 './deploy/validate-deployment.sh production'

# 5. Document incident
./deploy/audit-deployment.sh production incident sev1 initiated
```

### Communication Template

```
🚨 Incident Alert

Severity: SEV1
Status: Investigating
Impact: API unavailable
Started: 2024-11-10 14:30 UTC

Timeline:
14:30 - Incident detected
14:32 - Investigation started
14:35 - Rollback initiated

Current actions:
- Rolling back to v1.2.2
- Monitoring health checks

Next update: 15:00 UTC
```

---

**Last Updated**: 2024-11-10  
**Document Owner**: DevOps Team  
**Emergency Contact**: +1-555-0100
