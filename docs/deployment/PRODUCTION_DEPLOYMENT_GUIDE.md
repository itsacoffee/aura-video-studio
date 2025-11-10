# Production Deployment Guide

## Overview

This guide covers the complete production deployment process for Aura Video Studio, including deployment strategies, rollback procedures, and operational best practices.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Deployment Strategies](#deployment-strategies)
- [Deployment Process](#deployment-process)
- [Rollback Procedures](#rollback-procedures)
- [Monitoring and Validation](#monitoring-and-validation)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Access

- GitHub repository write access
- Cloud provider credentials (Azure/AWS/GCP)
- Container registry access
- Kubernetes cluster access (if using K8s)
- Production environment secrets

### Required Tools

```bash
# Install required tools
# Docker
docker --version  # >= 24.0.0

# Kubernetes CLI (if using K8s)
kubectl version --client  # >= 1.28.0

# Azure CLI (if using Azure)
az --version  # >= 2.50.0

# Terraform (for IaC)
terraform --version  # >= 1.5.0

# GitHub CLI
gh --version  # >= 2.30.0
```

### Environment Configuration

Ensure all required environment variables and secrets are configured:

- `DOCKER_REGISTRY`: Container registry URL
- `REDIS_PASSWORD`: Redis authentication password
- `AURA_OPENAI_API_KEY`: OpenAI API key
- `AURA_STABILITY_API_KEY`: Stability AI API key
- API keys for other providers
- `SENTRY_DSN`: Error tracking DSN
- `APPLICATION_INSIGHTS_KEY`: Azure monitoring key

## Deployment Strategies

### 1. Blue-Green Deployment (Default)

**Best for**: Production deployments requiring zero downtime

**Process**:
1. Deploy new version to inactive environment (green)
2. Validate health and functionality
3. Switch traffic from active (blue) to new (green)
4. Monitor for issues
5. Keep old environment for quick rollback

**Command**:
```bash
./deploy/blue-green-deploy.sh production v1.2.3
```

**Pros**:
- Zero downtime
- Instant rollback capability
- Full validation before traffic switch

**Cons**:
- Requires double infrastructure temporarily
- Database migrations need special handling

### 2. Canary Deployment

**Best for**: High-risk changes, gradual rollout

**Process**:
1. Deploy to 5% of instances
2. Monitor for 10 minutes
3. Increase to 50% of instances
4. Monitor for 10 minutes
5. Deploy to 100% of instances

**Command**:
```bash
./deploy/canary-deploy.sh production 15m
```

**Pros**:
- Gradual exposure to production traffic
- Early detection of issues
- Automatic rollback on failure

**Cons**:
- Longer deployment time (30-40 minutes)
- Requires careful monitoring

### 3. Rolling Deployment

**Best for**: Non-critical updates, smaller deployments

**Process**:
1. Deploy to one instance at a time
2. Validate health after each instance
3. Continue to next instance
4. Rollback all on failure

**Command**:
```bash
./deploy/rolling-deploy.sh production v1.2.3
```

**Pros**:
- No additional infrastructure needed
- Gradual rollout

**Cons**:
- Mixed versions during deployment
- Slower rollback

## Deployment Process

### Automated Deployment (Recommended)

#### Via GitHub Actions

1. **Tag-based deployment** (automatic on version tag):
```bash
# Create and push a version tag
git tag v1.2.3
git push origin v1.2.3

# GitHub Actions will automatically:
# - Build and test
# - Create Docker images
# - Deploy to staging
# - Wait for approval
# - Deploy to production
```

2. **Manual deployment** (workflow dispatch):
```bash
# Using GitHub CLI
gh workflow run production-deploy.yml \
  --field environment=production \
  --field version=v1.2.3 \
  --field deployment_strategy=blue-green

# Or via GitHub UI:
# Actions > Production Deployment Pipeline > Run workflow
```

### Manual Deployment

#### Step 1: Pre-deployment Checks

```bash
# 1. Verify clean working directory
git status

# 2. Check version to deploy
VERSION="v1.2.3"

# 3. Run pre-deployment validation
./deploy/validate-deployment.sh staging

# 4. Create deployment audit record
./deploy/audit-deployment.sh production $VERSION blue-green initiated
```

#### Step 2: Build and Push Images

```bash
# 1. Build production images
docker build -f Aura.Api/Dockerfile.prod -t aura-api:$VERSION .
docker build -f Aura.Web/Dockerfile.prod -t aura-web:$VERSION ./Aura.Web

# 2. Tag for registry
docker tag aura-api:$VERSION $DOCKER_REGISTRY/aura-api:$VERSION
docker tag aura-web:$VERSION $DOCKER_REGISTRY/aura-web:$VERSION

# 3. Push to registry
docker push $DOCKER_REGISTRY/aura-api:$VERSION
docker push $DOCKER_REGISTRY/aura-web:$VERSION
```

#### Step 3: Deploy

```bash
# Deploy using blue-green strategy
./deploy/blue-green-deploy.sh production $VERSION

# Or deploy using canary strategy
./deploy/canary-deploy.sh production 15m
```

#### Step 4: Post-deployment Validation

```bash
# Run comprehensive validation
export DEPLOY_VERSION=$VERSION
./deploy/validate-deployment.sh production https://api.aura.studio

# Update audit trail
./deploy/audit-deployment.sh production $VERSION blue-green success update
```

## Rollback Procedures

### Automatic Rollback

Automatic rollback is triggered when:
- Health checks fail for 3 consecutive attempts
- Error rate exceeds 300% of baseline
- Response time exceeds threshold
- System status shows "Down"

### Manual Rollback

#### Quick Rollback (Blue-Green)

```bash
# Immediate traffic switch to previous environment
./deploy/rollback.sh production "Manual rollback due to [REASON]"

# Verify rollback
./deploy/validate-deployment.sh production
```

#### Version Rollback

```bash
# Rollback to specific version
PREVIOUS_VERSION="v1.2.2"

# Deploy previous version
./deploy/blue-green-deploy.sh production $PREVIOUS_VERSION

# Update audit trail
./deploy/audit-deployment.sh production $PREVIOUS_VERSION rollback success
```

### Database Rollback

If database migrations were applied:

```bash
# 1. Stop API instances
docker compose -p aura-production stop api

# 2. Restore database backup
cp data/aura.db.backup data/aura.db

# 3. Restart API
docker compose -p aura-production start api

# 4. Verify database state
curl -sf https://api.aura.studio/api/diagnostics/system | jq '.database'
```

## Monitoring and Validation

### Health Checks

```bash
# Liveness check (is the service running?)
curl https://api.aura.studio/health/live

# Readiness check (can it serve traffic?)
curl https://api.aura.studio/health/ready

# System health (detailed status)
curl https://api.aura.studio/health/system
```

### Key Metrics to Monitor

1. **Response Times**
   - Target: < 2000ms (p95)
   - Alert threshold: > 5000ms

2. **Error Rate**
   - Target: < 1%
   - Alert threshold: > 5%

3. **Availability**
   - Target: 99.9%
   - Alert threshold: < 99%

4. **Resource Utilization**
   - CPU: < 70%
   - Memory: < 80%
   - Disk: < 85%

### Monitoring Tools

- **Application Insights**: Real-time application monitoring
- **Azure Monitor**: Infrastructure metrics
- **Sentry**: Error tracking and alerting
- **Custom dashboards**: `/api/diagnostics/metrics`

## Troubleshooting

### Common Issues

#### 1. Deployment Hangs

**Symptom**: Deployment script doesn't progress

**Solution**:
```bash
# Check container status
docker ps -a

# Check logs
docker logs aura-api-blue
docker logs aura-web-blue

# Check health endpoints
curl localhost:5005/health/live
```

#### 2. Health Checks Failing

**Symptom**: "Health check failed" errors

**Solution**:
```bash
# Check API logs
docker logs --tail 100 aura-api-blue

# Check database connectivity
docker exec aura-api-blue dotnet Aura.Api.dll --test-db

# Check Redis connectivity
docker exec aura-redis redis-cli ping
```

#### 3. Traffic Not Switching

**Symptom**: Old version still serving traffic

**Solution**:
```bash
# Check load balancer configuration
docker exec aura-loadbalancer cat /etc/nginx/upstream.conf

# Reload nginx
docker exec aura-loadbalancer nginx -s reload

# Verify upstream
curl -I https://api.aura.studio/api/version
```

#### 4. Database Migration Failures

**Symptom**: Database migration errors in logs

**Solution**:
```bash
# 1. Create backup
cp data/aura.db data/aura.db.backup

# 2. Check migration status
docker exec aura-api-blue dotnet ef migrations list

# 3. Rollback migration if needed
docker exec aura-api-blue dotnet ef database update [PREVIOUS_MIGRATION]

# 4. Restart API
docker restart aura-api-blue
```

## Emergency Procedures

### Critical Outage Response

1. **Immediate Actions** (< 5 minutes)
   ```bash
   # Execute rollback
   ./deploy/rollback.sh production "Critical outage"
   
   # Verify service restoration
   ./deploy/validate-deployment.sh production
   ```

2. **Incident Communication**
   - Notify stakeholders
   - Update status page
   - Create incident ticket

3. **Post-Incident**
   - Root cause analysis
   - Update runbooks
   - Implement preventive measures

### Degraded Performance Response

1. **Assess Impact**
   ```bash
   # Check current metrics
   curl https://api.aura.studio/api/diagnostics/metrics
   
   # Check resource utilization
   docker stats
   ```

2. **Scale Up (if needed)**
   ```bash
   # Increase replicas
   docker compose -p aura-production up -d --scale api=4
   ```

3. **Investigate Root Cause**
   - Check logs for errors
   - Review recent changes
   - Analyze performance metrics

## Best Practices

### Pre-deployment

- [ ] Review all changes in PR
- [ ] Ensure all tests pass
- [ ] Update version numbers
- [ ] Generate changelog
- [ ] Backup production database
- [ ] Schedule deployment during low-traffic period
- [ ] Notify team of deployment window

### During Deployment

- [ ] Monitor health checks continuously
- [ ] Watch error rates and logs
- [ ] Keep rollback plan ready
- [ ] Document any issues encountered

### Post-deployment

- [ ] Verify all features working
- [ ] Check monitoring dashboards
- [ ] Update deployment audit log
- [ ] Monitor for 24 hours
- [ ] Remove old containers/images
- [ ] Document lessons learned

## Compliance and Audit

### Deployment Approval

Production deployments require:
- Code review approval
- QA sign-off
- Change management ticket
- Deployment window approval

### Audit Trail

All deployments are logged to:
- `deploy/deployment-audit.log` (CSV format)
- `deploy/audit-records/*.json` (JSON format)

Generate audit report:
```bash
./deploy/audit-deployment.sh production v1.2.3 blue-green success report
```

### Compliance Checks

- ✅ All secrets stored securely in Key Vault
- ✅ No hardcoded credentials
- ✅ HTTPS enforced for all endpoints
- ✅ Security headers configured
- ✅ Rate limiting enabled
- ✅ Audit logging enabled

## Support and Escalation

### Deployment Support

- **Primary**: DevOps team
- **Secondary**: Platform team
- **On-call**: [On-call schedule]

### Escalation Path

1. Level 1: Team lead
2. Level 2: Engineering manager
3. Level 3: CTO

### Contact Information

- Slack: #aura-deployments
- Email: devops@aura.studio
- PagerDuty: Production alerts

---

**Last Updated**: 2024-11-10  
**Document Owner**: DevOps Team  
**Review Cycle**: Monthly
