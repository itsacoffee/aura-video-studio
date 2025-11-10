# Security Incident Response Runbook

## Overview

This runbook provides step-by-step procedures for responding to security incidents in Aura Video Studio.

## Table of Contents

- [Incident Classification](#incident-classification)
- [Response Team](#response-team)
- [General Response Procedure](#general-response-procedure)
- [Specific Incident Types](#specific-incident-types)
- [Communication Plan](#communication-plan)
- [Post-Incident Activities](#post-incident-activities)

## Incident Classification

### Severity Levels

#### SEV1 - Critical
- **Impact**: Service down, data breach, or complete compromise
- **Examples**: Database breach, mass data exfiltration, ransomware
- **Response Time**: Immediate (< 15 minutes)
- **Notification**: All stakeholders

#### SEV2 - High
- **Impact**: Significant degradation or unauthorized access
- **Examples**: Compromised API keys, DDoS attack, privilege escalation
- **Response Time**: < 1 hour
- **Notification**: Security team + management

#### SEV3 - Medium
- **Impact**: Limited functionality impact or potential vulnerability
- **Examples**: Failed login attempts, rate limit abuse, suspicious patterns
- **Response Time**: < 4 hours
- **Notification**: Security team

#### SEV4 - Low
- **Impact**: Minimal risk or information disclosure
- **Examples**: Scanner probes, minor configuration issues
- **Response Time**: < 24 hours
- **Notification**: Security team

## Response Team

### Roles and Responsibilities

#### Incident Commander
- Overall coordination
- Decision making
- Stakeholder communication
- **Contact**: [Your contact information]

#### Technical Lead
- Technical investigation
- System access and recovery
- Evidence preservation
- **Contact**: [Your contact information]

#### Communications Lead
- Internal/external communication
- Documentation
- Stakeholder updates
- **Contact**: [Your contact information]

#### Security Analyst
- Log analysis
- Threat intelligence
- Forensics support
- **Contact**: [Your contact information]

## General Response Procedure

### Phase 1: Detection & Triage (0-15 minutes)

#### 1.1 Initial Alert
- [ ] Receive and acknowledge alert
- [ ] Classify severity level
- [ ] Assign Incident Commander
- [ ] Create incident ticket

#### 1.2 Initial Assessment
```bash
# Check system health
curl http://localhost:5005/health

# Check audit logs
tail -f logs/audit-*.log

# Check error logs
tail -f logs/errors-*.log

# Check active connections
netstat -an | grep ESTABLISHED
```

#### 1.3 Incident Classification
- [ ] Determine incident type
- [ ] Assess scope and impact
- [ ] Classify severity
- [ ] Activate response team

### Phase 2: Containment (15 minutes - 2 hours)

#### 2.1 Immediate Actions
- [ ] Document timeline
- [ ] Preserve evidence
- [ ] Stop active threats
- [ ] Isolate affected systems

#### 2.2 Short-term Containment
- [ ] Block malicious IPs
- [ ] Disable compromised accounts
- [ ] Rotate affected secrets
- [ ] Enable additional logging

#### 2.3 System Stabilization
- [ ] Verify service availability
- [ ] Check data integrity
- [ ] Monitor for continued activity
- [ ] Update stakeholders

### Phase 3: Eradication (2-24 hours)

#### 3.1 Root Cause Analysis
- [ ] Identify attack vector
- [ ] Determine extent of compromise
- [ ] Find all affected systems
- [ ] Review audit logs

#### 3.2 Removal
- [ ] Remove malicious code
- [ ] Close vulnerabilities
- [ ] Patch systems
- [ ] Update configurations

#### 3.3 Verification
- [ ] Security scan all systems
- [ ] Verify no persistence mechanisms
- [ ] Test security controls
- [ ] Confirm remediation

### Phase 4: Recovery (1-3 days)

#### 4.1 System Restoration
- [ ] Restore from clean backup
- [ ] Redeploy services
- [ ] Verify functionality
- [ ] Monitor closely

#### 4.2 Validation
- [ ] Run security tests
- [ ] Verify logs normal
- [ ] Check performance
- [ ] User acceptance testing

#### 4.3 Documentation
- [ ] Update incident timeline
- [ ] Document actions taken
- [ ] Record lessons learned
- [ ] Update runbooks

### Phase 5: Post-Incident (1-2 weeks)

#### 5.1 Analysis
- [ ] Conduct post-mortem
- [ ] Review response effectiveness
- [ ] Identify improvements
- [ ] Update documentation

#### 5.2 Remediation
- [ ] Implement improvements
- [ ] Update security controls
- [ ] Train team members
- [ ] Update procedures

## Specific Incident Types

### 1. Compromised API Keys

#### Detection Signs
- Unusual API usage patterns
- Requests from unexpected IPs
- Excessive API calls
- Failed authentication attempts

#### Response Steps

1. **Immediate Actions**
```bash
# Review API key usage
grep "API key" logs/audit-*.log

# Check for suspicious patterns
grep "authentication failed" logs/audit-*.log

# Review rate limiting
grep "rate limit exceeded" logs/aura-api-*.log
```

2. **Containment**
```bash
# Rotate all API keys in Key Vault
az keyvault secret set \
  --vault-name aura-prod-vault \
  --name "OpenAI-ApiKey" \
  --value "sk-new-key-..."

# Wait for automatic refresh (30 minutes) or restart service
kubectl rollout restart deployment/aura-api
```

3. **Investigation**
- Review audit logs for API key access
- Check for data exfiltration
- Identify compromised keys
- Determine attack vector

4. **Recovery**
- Monitor new key usage
- Review billing for abuse
- Update key management procedures
- Enhance monitoring

### 2. Data Breach

#### Detection Signs
- Unusual database queries
- Large data exports
- Unauthorized data access
- External alerts

#### Response Steps

1. **Immediate Actions**
```bash
# Check database connections
sqlite3 aura.db "SELECT * FROM sqlite_master WHERE type='table';"

# Review data access logs
grep "sensitive data" logs/audit-*.log

# Check for bulk exports
grep "export\|download" logs/aura-api-*.log
```

2. **Containment**
```bash
# Block suspicious IPs
# Add to firewall rules

# Disable affected accounts
# Update user permissions

# Enable additional logging
# Increase audit detail
```

3. **Investigation**
- Determine what data was accessed
- Identify affected users
- Review access patterns
- Check for data exfiltration

4. **Recovery**
- Notify affected users
- Comply with breach notification laws
- Offer mitigation (credit monitoring)
- Enhance data protection

### 3. DDoS Attack

#### Detection Signs
- Service degradation
- High CPU/memory usage
- Excessive rate limiting
- Network saturation

#### Response Steps

1. **Immediate Actions**
```bash
# Check rate limiting
grep "rate limit" logs/aura-api-*.log | wc -l

# Identify attack sources
awk '{print $1}' access.log | sort | uniq -c | sort -rn | head -20

# Check system resources
top -n 1
free -h
df -h
```

2. **Containment**
```bash
# Enable more restrictive rate limits
# Update appsettings.json
"IpRateLimiting": {
  "GeneralRules": [
    {
      "Endpoint": "*",
      "Period": "1m",
      "Limit": 10
    }
  ]
}

# Block attacking IPs
iptables -A INPUT -s <attacker-ip> -j DROP

# Enable DDoS protection
# Configure CDN or DDoS mitigation service
```

3. **Investigation**
- Analyze attack patterns
- Identify bot signatures
- Check for amplification
- Determine motivation

4. **Recovery**
- Restore normal rate limits gradually
- Monitor for continued activity
- Update DDoS defenses
- Document attack patterns

### 4. Malware Detection

#### Detection Signs
- Unusual process activity
- Unexpected network connections
- Modified system files
- Antivirus alerts

#### Response Steps

1. **Immediate Actions**
```bash
# Isolate affected system
# Disconnect from network

# Identify suspicious processes
ps aux | grep -v "^\[" | sort -k3 -rn | head -20

# Check for unauthorized files
find /app -type f -mtime -1

# Scan for malware
# Use appropriate security tools
```

2. **Containment**
- Quarantine infected systems
- Block malware command & control
- Disable affected services
- Prevent spread

3. **Investigation**
- Identify malware type
- Determine infection vector
- Find all infected systems
- Check for persistence mechanisms

4. **Recovery**
- Remove malware
- Restore from clean backup
- Patch vulnerabilities
- Update security controls

### 5. Insider Threat

#### Detection Signs
- Unusual access patterns
- Data exfiltration attempts
- Permission escalation
- After-hours access

#### Response Steps

1. **Immediate Actions**
```bash
# Review user activity
grep "userId" logs/audit-*.log | grep "<suspicious-user>"

# Check data access
grep "sensitive data access" logs/audit-*.log

# Review configuration changes
grep "configuration changed" logs/audit-*.log
```

2. **Containment**
- Suspend suspicious accounts
- Revoke access tokens
- Monitor continued activity
- Preserve evidence

3. **Investigation**
- Review complete activity history
- Check for data exfiltration
- Identify motive
- Coordinate with HR/Legal

4. **Recovery**
- Rotate all secrets accessed
- Update access controls
- Enhance monitoring
- Review policies

## Communication Plan

### Internal Communication

#### Incident Declaration
```
Subject: [SEV1] Security Incident - [Brief Description]

An incident has been declared at [TIME].

Severity: SEV1
Type: [Incident Type]
Impact: [Description]
Status: [Detection/Containment/Eradication/Recovery]

Incident Commander: [Name]
Bridge: [Conference Link]

Updates will be provided every [FREQUENCY].
```

#### Status Updates
```
Subject: [SEV1] Incident Update - [TIME]

Current Status: [Status]
Actions Taken: [List]
Next Steps: [List]
ETA to Resolution: [Estimate]
Impact: [Current Impact]
```

#### Resolution Notification
```
Subject: [SEV1] Incident Resolved - [Brief Description]

The incident declared at [START TIME] has been resolved at [END TIME].

Summary: [Brief description]
Root Cause: [Cause]
Resolution: [Actions taken]
Follow-up: [Post-incident review scheduled]
```

### External Communication

#### Customer Notification (if required)
```
Subject: Service Notice - [Brief Description]

We are investigating reports of [ISSUE].

What we know:
- [Status]
- [Impact]
- [Actions being taken]

What you should do:
- [Recommendations]

We will provide updates at [FREQUENCY].
For questions: support@aura.studio
```

#### Data Breach Notification (if required)
```
Subject: Important Security Notice

We are writing to inform you of a security incident that may have affected your information.

What happened: [Description]
What information was involved: [Details]
What we are doing: [Actions]
What you should do: [Recommendations]

For more information: [Contact details]
```

## Post-Incident Activities

### Post-Mortem Template

```markdown
# Post-Incident Review: [Incident ID]

## Incident Summary
- **Date**: [Date]
- **Severity**: [SEV1/2/3/4]
- **Type**: [Type]
- **Duration**: [Duration]
- **Impact**: [Description]

## Timeline
| Time | Event | Action Taken |
|------|-------|-------------|
| [TIME] | [Event] | [Action] |

## Root Cause Analysis
### What happened?
[Description]

### Why did it happen?
[Causes]

### Contributing factors
- [Factor 1]
- [Factor 2]

## Response Effectiveness
### What went well?
- [Item 1]
- [Item 2]

### What could be improved?
- [Item 1]
- [Item 2]

## Action Items
| Action | Owner | Due Date | Status |
|--------|-------|----------|--------|
| [Action] | [Name] | [Date] | [Status] |

## Lessons Learned
[Key takeaways]

## Documentation Updates
- [ ] Update runbooks
- [ ] Update monitoring
- [ ] Update training materials
- [ ] Update security controls
```

### Follow-up Checklist

#### Immediate (1-3 days)
- [ ] Complete post-mortem
- [ ] Update documentation
- [ ] Share lessons learned
- [ ] Implement quick fixes

#### Short-term (1-2 weeks)
- [ ] Implement action items
- [ ] Update security controls
- [ ] Conduct training
- [ ] Review and test procedures

#### Long-term (1-3 months)
- [ ] Review metrics
- [ ] Update architecture
- [ ] Conduct security audit
- [ ] Test incident response

## Tools and Resources

### Log Locations
```
# Application logs
/app/logs/aura-api-*.log

# Error logs
/app/logs/errors-*.log

# Audit logs
/app/logs/audit-*.log

# Performance logs
/app/logs/performance-*.log
```

### Useful Commands
```bash
# Check service status
systemctl status aura-api

# View recent errors
tail -f logs/errors-*.log

# Check database
sqlite3 aura.db ".tables"

# Monitor network
netstat -tuln

# Check disk space
df -h

# Check memory
free -m

# List processes
ps aux

# Check open files
lsof -i
```

### Contact Information

#### Security Team
- **Email**: security@aura.studio
- **Phone**: [Phone]
- **Slack**: #security-incidents

#### On-Call Rotation
- **Primary**: [Name/Contact]
- **Secondary**: [Name/Contact]
- **Manager**: [Name/Contact]

#### External Resources
- **Azure Support**: [Contact]
- **Legal Counsel**: [Contact]
- **PR Team**: [Contact]
- **Insurance**: [Contact]

## Training and Drills

### Quarterly Drills
- [ ] Simulated breach
- [ ] DDoS response
- [ ] Key rotation
- [ ] Communication test

### Annual Review
- [ ] Update runbook
- [ ] Review procedures
- [ ] Test backups
- [ ] Security audit

### New Team Member Onboarding
- [ ] Review runbook
- [ ] Access credentials
- [ ] Role assignment
- [ ] Shadow incident

## Compliance and Legal

### Regulatory Requirements
- **GDPR**: Breach notification within 72 hours
- **CCPA**: Notice to affected individuals
- **SOC 2**: Incident documentation
- **PCI DSS**: If applicable

### Documentation Requirements
- Incident timeline
- Actions taken
- Data affected
- Notification records
- Remediation steps

### Legal Holds
- Preserve all evidence
- Document chain of custody
- Coordinate with legal
- Follow retention policies

## Continuous Improvement

### Metrics to Track
- Time to detect
- Time to contain
- Time to recover
- False positive rate
- Number of incidents
- Severity distribution

### Review Schedule
- **Weekly**: Incident review
- **Monthly**: Metrics review
- **Quarterly**: Procedure update
- **Annually**: Full audit

### Feedback Loop
1. Incident occurs
2. Response executed
3. Post-mortem conducted
4. Improvements identified
5. Procedures updated
6. Training conducted
7. Next incident (improved response)

## Additional Resources

- [NIST Incident Response Guide](https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-61r2.pdf)
- [SANS Incident Response](https://www.sans.org/reading-room/whitepapers/incident/)
- [Microsoft Security Response](https://www.microsoft.com/security/blog/microsoft-security-response-center/)
