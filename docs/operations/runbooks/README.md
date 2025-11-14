# Operational Runbooks

This directory contains operational runbooks for managing and troubleshooting Aura Video Studio in production and development environments.

## What is a Runbook?

A runbook is a step-by-step guide for handling common operational tasks, incidents, and scenarios. Each runbook provides:

- Clear problem description
- Diagnostic steps
- Resolution procedures
- Prevention measures

## Available Runbooks

### System Operations

- [Deployment](./deployment.md) - Deploying and updating Aura Video Studio
- Backup and Restore - Data backup and recovery procedures
- Monitoring - System monitoring and alerting setup
- Performance Tuning - Performance optimization guide

### Incident Response

- Service Degradation - Handling slow or degraded performance
- High Error Rates - Diagnosing and fixing error spikes
- Database Issues - Database connectivity and corruption
- Provider Failures - External provider outages and fallbacks

### Maintenance

- Log Management - Log rotation, archival, and analysis
- Database Maintenance - Vacuuming, indexing, optimization
- Cache Management - Redis cache operations and troubleshooting
- Dependency Updates - Updating dependencies safely

## Runbook Format

Each runbook follows this structure:

### 1. Overview
- Brief description of the issue or task
- Severity level (if applicable)
- Estimated time to resolve

### 2. Symptoms
- Observable symptoms or indicators
- Alerts that may trigger
- User-reported issues

### 3. Diagnosis
- Step-by-step diagnostic procedure
- Commands to run
- Expected vs. actual output
- Decision trees for different scenarios

### 4. Resolution
- Immediate actions to resolve the issue
- Step-by-step procedures
- Rollback procedures if applicable
- Verification steps

### 5. Prevention
- Root cause analysis
- Long-term fixes
- Monitoring improvements
- Documentation updates

### 6. References
- Related runbooks
- External documentation
- Support contacts

## Using Runbooks

### For Operators

1. **Identify the issue**: Match symptoms to appropriate runbook
2. **Follow diagnostic steps**: Gather information systematically
3. **Apply resolution**: Execute steps carefully, noting any deviations
4. **Verify fix**: Confirm the issue is resolved
5. **Document**: Update runbook if you discover new information

### For Developers

1. **Create runbooks** for new features that may fail in production
2. **Update runbooks** when making significant changes
3. **Test procedures** in staging before production
4. **Review runbooks** during post-mortems

## Quick Reference

### Common Commands

```bash
# Check API health
curl http://localhost:5005/api/v1/health

# View API logs
tail -f logs/aura-api-*.log

# View error logs only
tail -f logs/errors-*.log

# Check database size
sqlite3 aura.db "SELECT page_count * page_size as size FROM pragma_page_count(), pragma_page_size();"

# Check Redis connectivity
redis-cli ping

# Check disk space
df -h

# Check memory usage
free -h

# Check running processes
ps aux | grep -E "(dotnet|node|redis|ffmpeg)"
```

### Emergency Contacts

- **On-call Engineer**: Check PagerDuty
- **Database Admin**: [Contact info]
- **Infrastructure Team**: [Contact info]
- **Security Team**: [Contact info]

### Escalation Path

1. **Level 1** (0-5 min): Follow runbook procedures
2. **Level 2** (5-15 min): Consult with senior engineer
3. **Level 3** (15-30 min): Escalate to on-call manager
4. **Level 4** (30+ min): Engage vendor support if needed

## Severity Levels

### SEV-1: Critical
- Complete service outage
- Data loss risk
- Security breach
- Response time: Immediate

### SEV-2: High
- Significant feature degradation
- High error rates (>5%)
- Performance severely impacted
- Response time: <15 minutes

### SEV-3: Medium
- Minor feature issues
- Intermittent errors
- Performance slightly degraded
- Response time: <1 hour

### SEV-4: Low
- Cosmetic issues
- Known workarounds available
- No user impact
- Response time: Next business day

## Maintenance Windows

Schedule maintenance during low-usage periods:

- **Preferred**: Sunday 2:00 AM - 6:00 AM (local time)
- **Alternative**: Weekday 2:00 AM - 4:00 AM
- **Emergency**: Anytime with manager approval

## Change Management

### Pre-Change

- [ ] Change request approved
- [ ] Runbook reviewed and updated
- [ ] Rollback plan documented
- [ ] Stakeholders notified
- [ ] Backup completed

### During Change

- [ ] Follow runbook steps
- [ ] Document deviations
- [ ] Monitor key metrics
- [ ] Communicate status updates

### Post-Change

- [ ] Verify successful deployment
- [ ] Monitor for 24 hours
- [ ] Update documentation
- [ ] Conduct post-mortem if issues
- [ ] Archive change logs

## Contributing to Runbooks

### When to Create a Runbook

Create a runbook when:
- A new failure mode is discovered
- A manual procedure is performed repeatedly
- An incident requires complex diagnosis
- New features introduce operational complexity

### Runbook Standards

- **Clear titles**: Describe the problem or task
- **Action-oriented**: Use imperative verbs
- **Tested procedures**: Verify steps work before documenting
- **Up-to-date**: Review quarterly and after incidents
- **Accessible**: Use simple language, avoid jargon

### Review Process

1. Draft runbook using template
2. Test procedures in staging
3. Peer review by team member
4. Approval by operations lead
5. Publish and announce to team

## Additional Resources

- [Troubleshooting Guide](../../../TROUBLESHOOTING.md)
- [Architecture Documentation](../../architecture/ARCHITECTURE.md)
- [API Documentation](../../api/README.md)
- Monitoring Setup

## Feedback

Runbooks are living documents. If you find:
- Inaccurate information
- Missing steps
- Outdated procedures
- Unclear instructions

Please submit an issue or PR to update the runbook.

---

**Last Updated**: 2024-11-10  
**Maintained by**: Operations Team
