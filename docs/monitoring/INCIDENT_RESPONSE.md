# Incident Response Procedures

## Overview

This document outlines the incident response procedures for Aura production operations. Following these procedures ensures rapid resolution, clear communication, and continuous improvement.

## Incident Severity Levels

### SEV-1: Critical

**Definition**: Complete service outage or severe degradation affecting all users

**Examples**:
- API completely unavailable
- Data loss or corruption
- Security breach
- Payment system failure

**Response**:
- **Time to Acknowledge**: < 5 minutes
- **Initial Response Time**: Immediate
- **Communication**: Real-time updates every 15 minutes
- **Escalation**: Automatic page to on-call engineer
- **All-Hands**: Mobilize all available engineers
- **Post-Mortem**: Required within 48 hours

### SEV-2: Major

**Definition**: Significant service degradation affecting multiple users

**Examples**:
- API partially unavailable
- SLO breach (< 99% availability)
- Critical feature failure
- Major performance degradation

**Response**:
- **Time to Acknowledge**: < 15 minutes
- **Initial Response Time**: < 1 hour
- **Communication**: Updates every 30 minutes
- **Escalation**: Page on-call if not resolved in 1 hour
- **Post-Mortem**: Required within 1 week

### SEV-3: Minor

**Definition**: Isolated issues with workarounds available

**Examples**:
- Non-critical feature impaired
- Intermittent errors
- Performance degradation for edge cases
- Provider health warnings

**Response**:
- **Time to Acknowledge**: < 1 hour
- **Initial Response Time**: Within business day
- **Communication**: Updates as needed
- **Escalation**: None unless escalates to SEV-2
- **Post-Mortem**: Optional

## Incident Response Phases

### 1. Detection

**How Incidents Are Detected**:
- Automated alerts (PagerDuty, Slack)
- Synthetic monitoring failures
- User reports (support tickets, social media)
- Internal team observations

**First Actions**:
1. Acknowledge the alert (stops re-paging)
2. Assess severity
3. Create incident ticket
4. Join incident channel (Slack: #incident-active)

### 2. Triage

**Assess Impact**:
- How many users are affected?
- What functionality is broken?
- Is this getting worse?
- What's the financial impact?

**Gather Initial Data**:
```bash
# Check overall system health
curl http://localhost:5005/api/health

# Check firing alerts
curl http://localhost:5005/api/monitoring/alerts/firing

# Check recent errors
tail -n 100 logs/errors-$(date +%Y-%m-%d).log

# Check recent deployments
git log --since="2 hours ago" --oneline
```

**Determine Severity**:
Use the severity matrix above to classify the incident.

### 3. Communication

**Incident Channel**: #incident-active on Slack

**Communication Template**:
```
🚨 INCIDENT DETECTED

Severity: SEV-X
Summary: <Brief description>
Impact: <What's broken and for whom>
Detected: <Timestamp>
Status: Investigating

Updates will be posted every <15/30/60> minutes.
```

**Update Template**:
```
⏰ UPDATE [HH:MM]

Current Status: <Investigating|Mitigating|Resolved>
Actions Taken: <What we've done>
Next Steps: <What we're doing next>
ETA: <Best estimate>
```

**Resolution Template**:
```
✅ RESOLVED [HH:MM]

Summary: <What happened>
Root Cause: <Why it happened>
Resolution: <How we fixed it>
Duration: <Total incident time>

Post-mortem will be completed by: <Date>
```

### 4. Mitigation

**Goal**: Restore service ASAP (fix root cause later if needed)

**Mitigation Strategies**:

1. **Rollback Recent Deployment**
   ```bash
   # Rollback to previous version
   git log -10 --oneline
   git checkout <previous-commit>
   docker build -t aura-api:rollback .
   docker-compose up -d
   ```

2. **Scale Up Resources**
   ```bash
   # Increase container count
   docker-compose up -d --scale api=5
   
   # Or scale in Azure/AWS
   az webapp scale --resource-group aura --name aura-api --instance-count 5
   ```

3. **Switch to Backup Provider**
   ```bash
   # Disable failing provider
   curl -X POST http://localhost:5005/api/providers/disable \
     -H "Content-Type: application/json" \
     -d '{"provider": "OpenAI", "reason": "High error rate"}'
   
   # Verify failover
   curl http://localhost:5005/api/health/providers
   ```

4. **Clear Cache**
   ```bash
   # If caching issue
   curl -X POST http://localhost:5005/api/cache/clear
   ```

5. **Restart Services**
   ```bash
   # Last resort
   docker-compose restart
   ```

### 5. Resolution

**Verify Service Restored**:
```bash
# Check health endpoints
curl http://localhost:5005/api/health
curl http://localhost:5005/api/monitoring/health/synthetic

# Check error rate
curl http://localhost:5005/api/monitoring/metrics/histogram/api.errors.5xx

# Run smoke tests
./scripts/smoke-tests.sh
```

**Clear Alerts**:
- Most alerts auto-resolve when metrics return to normal
- Manually acknowledge any persistent alerts

**Communicate Resolution**:
- Post resolution message to #incident-active
- Notify users via status page
- Close incident ticket

### 6. Post-Mortem

**Within 48 Hours for SEV-1, 1 Week for SEV-2**

**Post-Mortem Template**:

````markdown
# Incident Post-Mortem: <Title>

## Incident Summary
- **Date**: YYYY-MM-DD
- **Duration**: <Total time>
- **Severity**: SEV-X
- **Impact**: <Users affected, functionality impaired>

## Timeline

| Time | Event |
|------|-------|
| 14:30 | Alert fired: API Availability Below SLO |
| 14:32 | On-call acknowledged, began investigation |
| 14:40 | Identified root cause: Database connection pool exhausted |
| 14:45 | Mitigation: Restarted API, increased connection pool size |
| 15:00 | Service restored, monitoring for stability |
| 15:30 | Incident closed |

## Root Cause

<Detailed explanation of what went wrong and why>

Example:
- Database connection pool was configured for max 50 connections
- Increased traffic from marketing campaign exceeded capacity
- Connections were not being released properly due to bug in new code
- Led to connection pool exhaustion and API failures

## Impact Assessment

- **Users Affected**: ~5,000 (estimated)
- **Duration**: 1 hour
- **SLO Impact**: 99.5% availability (below 99.9% target)
- **Financial Impact**: ~$500 in lost revenue
- **Reputation Impact**: 12 support tickets, 3 tweets

## What Went Well

- ✅ Alert fired within 2 minutes of issue
- ✅ On-call engineer responded quickly
- ✅ Mitigation applied within 15 minutes
- ✅ Clear communication in incident channel

## What Went Wrong

- ❌ Connection pool size not adequately tested under load
- ❌ Code review didn't catch connection leak
- ❌ Monitoring didn't alert on connection pool usage
- ❌ Rollback took longer than expected (manual process)

## Action Items

| Action | Owner | Due Date | Status |
|--------|-------|----------|--------|
| Add alert for connection pool usage | @alice | 2025-11-15 | ✅ Done |
| Add load testing to CI/CD | @bob | 2025-11-20 | 🔄 In Progress |
| Fix connection leak bug | @charlie | 2025-11-12 | ✅ Done |
| Automate rollback process | @david | 2025-11-30 | 📋 To Do |
| Increase connection pool to 200 | @alice | 2025-11-11 | ✅ Done |

## Lessons Learned

1. **Load testing is critical**: Always test new features under realistic load
2. **Monitor everything**: Connection pools, thread pools, file descriptors
3. **Fast rollbacks are essential**: Automate deployment/rollback
4. **Code reviews should check resource usage**: Not just functionality

## Questions Raised

1. Should we implement circuit breakers for database connections?
2. Do we need auto-scaling based on connection pool usage?
3. Should we have a staging environment that mirrors production load?

## Related Incidents

- None (first incident of this type)
````

**Distribute Post-Mortem**:
- Share with engineering team
- Present at weekly incident review
- Add to incident knowledge base

## On-Call Responsibilities

### Primary On-Call

**Responsibilities**:
- Monitor alerts 24/7
- Acknowledge incidents within 5 minutes (SEV-1)
- Lead incident response
- Communicate status updates
- Complete post-mortem

**Rotation**:
- 1-week shifts
- Hand-off meeting at shift change
- Calendar invites sent automatically

**Escalation**:
- If unable to resolve SEV-1 in 30 minutes, escalate to secondary
- If unable to resolve SEV-2 in 2 hours, escalate to secondary

### Secondary On-Call

**Responsibilities**:
- Backup for primary on-call
- Respond to escalations
- Available for consultation
- Take over if primary is unavailable

### Escalation Path

```
Level 1: Primary On-Call Engineer
   ↓ (30 min for SEV-1, 2 hr for SEV-2)
Level 2: Secondary On-Call Engineer
   ↓ (1 hr for SEV-1, 4 hr for SEV-2)
Level 3: Engineering Manager
   ↓ (2 hr for SEV-1)
Level 4: VP of Engineering / CTO
```

## Communication Guidelines

### Internal Communication (Slack)

**Channels**:
- `#incident-active`: Active incident coordination
- `#incident-archive`: Historical incidents
- `#engineering`: General team updates
- `#alerts`: Alert notifications

**Best Practices**:
- Use threads to keep discussions organized
- Pin important messages (mitigation steps, status)
- Use emojis for quick status:
  - 🚨 New incident
  - 🔍 Investigating
  - 🛠️ Mitigating
  - ✅ Resolved
  - ⚠️ Degraded

### External Communication (Users)

**Status Page** (status.aura.studio):
- Update within 15 minutes of incident
- Provide regular updates
- Clear, non-technical language

**Example Status Page Update**:
```
⚠️ Partial Service Disruption

We are currently investigating issues with video generation.
Some users may experience delays or failures.

Our team is actively working on a resolution.

Updated: 2025-11-10 14:45 UTC
Next update: 2025-11-10 15:00 UTC
```

**Social Media**:
- Tweet from @AuraStudio account for SEV-1
- Acknowledge user reports
- Link to status page

**Email** (for prolonged outages):
- Send to all active users
- Apologize, explain, provide ETA
- Offer compensation if appropriate

## Incident Tooling

### Required Tools

1. **PagerDuty**: Alert routing and escalation
2. **Slack**: Real-time communication
3. **Azure Portal**: Infrastructure management
4. **GitHub**: Code deployment and rollback
5. **Monitoring Dashboards**: Observability

### Quick Links

Keep these bookmarked:

- **Monitoring Dashboard**: https://portal.azure.com/...
- **Logs**: https://portal.azure.com/...
- **Runbooks**: https://github.com/Coffee285/aura-video-studio/blob/main/docs/runbooks/
- **Status Page Admin**: https://status.aura.studio/admin
- **PagerDuty**: https://aura.pagerduty.com

### Useful Commands

```bash
# Quick health check
curl -s http://localhost:5005/api/health | jq .

# Check firing alerts
curl -s http://localhost:5005/api/monitoring/alerts/firing | jq .

# View recent errors
tail -n 50 logs/errors-$(date +%Y-%m-%d).log

# Check system resources
docker stats

# View recent deployments
git log --since="6 hours ago" --oneline

# Rollback to previous version
git checkout HEAD~1 && docker-compose up -d

# Scale up
docker-compose up -d --scale api=5

# Restart services
docker-compose restart api
```

## Testing Incident Response

### Fire Drills

**Monthly Drill**:
- Simulate SEV-2 incident
- Practice detection, mitigation, communication
- Review performance afterward

**Example Scenarios**:
1. Database connection pool exhaustion
2. Provider API outage
3. Deployment introduces bug
4. Sudden traffic spike
5. Disk space exhaustion

### Chaos Engineering

**Inject Failures**:
```bash
# Kill random container
docker ps | tail -n +2 | shuf -n 1 | awk '{print $1}' | xargs docker kill

# Introduce network latency
tc qdisc add dev eth0 root netem delay 500ms

# Fill disk space
dd if=/dev/zero of=/tmp/fillfile bs=1M count=1000
```

## Incident Metrics

### Track These Metrics

1. **MTTD** (Mean Time To Detect): Alert fires → Incident acknowledged
2. **MTTR** (Mean Time To Resolve): Incident start → Service restored
3. **False Positive Rate**: Alerts that were not incidents
4. **Incident Frequency**: Incidents per week/month
5. **SLO Compliance**: % of time meeting SLO targets

### Monthly Report

```markdown
## Incident Report: November 2025

- **Total Incidents**: 8
  - SEV-1: 0
  - SEV-2: 2
  - SEV-3: 6

- **MTTD**: 3.2 minutes (target: < 5 min)
- **MTTR**: 
  - SEV-2: 45 minutes (target: < 1 hour)
  - SEV-3: 4 hours (target: < 1 day)

- **SLO Compliance**: 99.87% (target: 99.9%)

- **Top Incident Causes**:
  1. Provider API failures (3)
  2. Performance issues (2)
  3. Deployment bugs (2)
  4. Infrastructure issues (1)

- **Action Items**:
  - Improve provider failover logic
  - Add more performance testing
  - Implement canary deployments
```

## Best Practices

### ✅ DO

- Acknowledge alerts immediately
- Communicate early and often
- Focus on mitigation first, root cause second
- Document everything in incident ticket
- Run regular fire drills
- Review incidents as a team

### ❌ DON'T

- Panic or guess wildly
- Make changes without documenting
- Skip post-mortems
- Blame individuals (blame processes)
- Fix and forget (learn from incidents)

## Further Reading

- [Monitoring Philosophy](./MONITORING_PHILOSOPHY.md)
- [Alert Creation Guide](./ALERT_CREATION_GUIDE.md)
- [Runbooks Index](./runbooks/INDEX.md)
- [On-Call Guide](./ON_CALL_GUIDE.md)

## Summary

Effective incident response requires:
1. **Fast Detection**: Automated monitoring and alerting
2. **Clear Process**: Follow established procedures
3. **Good Communication**: Keep stakeholders informed
4. **Quick Mitigation**: Restore service ASAP
5. **Continuous Learning**: Post-mortems and action items

Remember: **Incidents are learning opportunities. No blame, just improvement.**
