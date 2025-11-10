# Release Process Documentation

## Overview

This document describes the complete release process for Aura Video Studio, from planning to production deployment.

## Release Cycle

### Schedule

- **Minor Releases**: Every 2 weeks (sprints)
- **Patch Releases**: As needed for critical fixes
- **Major Releases**: Quarterly

### Release Calendar

| Quarter | Major Release | Minor Releases | 
|---------|--------------|----------------|
| Q1      | v2.0.0       | v2.1.0, v2.2.0, v2.3.0 |
| Q2      | v3.0.0       | v3.1.0, v3.2.0, v3.3.0 |
| Q3      | v4.0.0       | v4.1.0, v4.2.0, v4.3.0 |
| Q4      | v5.0.0       | v5.1.0, v5.2.0, v5.3.0 |

## Semantic Versioning

We follow [Semantic Versioning 2.0.0](https://semver.org/):

```
vMAJOR.MINOR.PATCH

Example: v1.2.3
- 1: Major version
- 2: Minor version
- 3: Patch version
```

### Version Bumping Rules

- **MAJOR**: Breaking changes (v1.0.0 → v2.0.0)
- **MINOR**: New features, backward compatible (v1.0.0 → v1.1.0)
- **PATCH**: Bug fixes, backward compatible (v1.0.0 → v1.0.1)

### Automatic Version Calculation

Use our semantic version script:

```bash
# Analyze commits and calculate next version
./scripts/release/semantic-version.sh

# Dry run (no changes)
./scripts/release/semantic-version.sh --dry-run
```

The script analyzes conventional commits:
- `feat:` → Minor version bump
- `fix:` → Patch version bump
- `BREAKING CHANGE:` or `feat!:` → Major version bump

## Release Planning

### 1. Feature Freeze

**Timeline**: 3 days before release

**Actions**:
- [ ] Freeze feature branch
- [ ] Create release branch: `release/v1.2.3`
- [ ] Update version numbers
- [ ] Generate changelog

```bash
# Create release branch
git checkout develop
git pull origin develop
git checkout -b release/v1.2.3

# Update version
./scripts/release/semantic-version.sh

# Generate changelog
./scripts/release/generate-changelog.sh v1.2.3
```

### 2. Testing Phase

**Timeline**: 2 days before release

**Actions**:
- [ ] Run full test suite
- [ ] Execute E2E tests
- [ ] Perform security scans
- [ ] Manual testing in staging
- [ ] Performance testing

```bash
# Run all tests
dotnet test Aura.sln --configuration Release

# Run E2E tests
cd Aura.Web && npm run test:e2e

# Security scan
./scripts/audit/scan-security.sh

# Deploy to staging
./deploy/blue-green-deploy.sh staging v1.2.3
```

### 3. Release Candidate

**Timeline**: 1 day before release

**Actions**:
- [ ] Create RC tag: `v1.2.3-rc.1`
- [ ] Deploy to staging
- [ ] Stakeholder review
- [ ] Final bug fixes

```bash
# Tag release candidate
git tag v1.2.3-rc.1
git push origin v1.2.3-rc.1

# Deploy to staging
gh workflow run production-deploy.yml \
  --field environment=staging \
  --field version=v1.2.3-rc.1
```

### 4. Release Day

**Actions**:
- [ ] Merge release branch to main
- [ ] Create release tag
- [ ] Deploy to production
- [ ] Monitor deployment
- [ ] Update documentation

```bash
# Merge to main
git checkout main
git merge release/v1.2.3 --no-ff
git push origin main

# Create release tag
git tag v1.2.3
git push origin v1.2.3

# This triggers production deployment workflow
```

## Conventional Commits

We use [Conventional Commits](https://www.conventionalcommits.org/) for automated changelog generation.

### Commit Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `ci`: CI/CD changes

### Examples

```bash
# Feature
git commit -m "feat(video): add support for 4K video rendering"

# Bug fix
git commit -m "fix(api): resolve null reference in video processor"

# Breaking change
git commit -m "feat(auth)!: change authentication to use OAuth 2.0

BREAKING CHANGE: All API clients must update to OAuth 2.0. The old API key authentication is no longer supported."

# Multiple scopes
git commit -m "feat(api,web): add real-time collaboration features"
```

## Changelog Generation

### Automatic Generation

```bash
# Generate changelog from commits
./scripts/release/generate-changelog.sh v1.2.3

# Output: CHANGELOG.md
```

### Changelog Format

```markdown
# Changelog

## [1.2.3] - 2024-11-10

### ✨ Features

- **video**: Add support for 4K video rendering (abc1234)
- **api**: Implement batch video processing (def5678)

### 🐛 Bug Fixes

- **api**: Resolve null reference in video processor (ghi9012)
- **web**: Fix UI glitch in timeline editor (jkl3456)

### ⚡ Performance Improvements

- **rendering**: Optimize video encoding by 40% (mno7890)

### 📝 Documentation

- **deployment**: Update production deployment guide (pqr1234)
```

## Release Checklist

### Pre-Release

- [ ] All tests passing
- [ ] Code review completed
- [ ] Security scan passed
- [ ] Performance benchmarks met
- [ ] Documentation updated
- [ ] Changelog generated
- [ ] Version numbers updated
- [ ] Release notes prepared
- [ ] Stakeholder approval obtained
- [ ] Change management ticket created

### Release

- [ ] Release branch merged to main
- [ ] Release tag created
- [ ] GitHub release created
- [ ] Docker images built and pushed
- [ ] Deployment to production initiated
- [ ] Health checks passing
- [ ] Monitoring dashboards verified
- [ ] Release announcement sent

### Post-Release

- [ ] Monitor for 24 hours
- [ ] Verify no critical errors
- [ ] Update release documentation
- [ ] Close release milestone
- [ ] Merge back to develop
- [ ] Thank contributors
- [ ] Collect feedback

## Hotfix Process

For critical production issues:

### 1. Create Hotfix Branch

```bash
# From main branch
git checkout main
git pull origin main
git checkout -b hotfix/v1.2.4

# Make fixes
git add .
git commit -m "fix(critical): resolve memory leak in video processor"
```

### 2. Test and Validate

```bash
# Run tests
dotnet test

# Deploy to staging
./deploy/blue-green-deploy.sh staging v1.2.4
```

### 3. Release Hotfix

```bash
# Merge to main
git checkout main
git merge hotfix/v1.2.4 --no-ff

# Tag release
git tag v1.2.4
git push origin main v1.2.4

# Merge back to develop
git checkout develop
git merge hotfix/v1.2.4 --no-ff
git push origin develop

# Delete hotfix branch
git branch -d hotfix/v1.2.4
git push origin --delete hotfix/v1.2.4
```

## Feature Flags

Use feature flags for gradual rollout of new features.

### Define Feature Flag

```csharp
// In code
if (await _featureFlagService.IsEnabledAsync("new_video_editor"))
{
    // New feature code
}
else
{
    // Old feature code
}
```

### Manage Feature Flags

```bash
# Enable feature for 10% of users
curl -X POST https://api.aura.studio/api/featureflags/new_video_editor/rollout \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"percentage": 10}'

# Enable for specific user
curl -X POST https://api.aura.studio/api/featureflags/new_video_editor/allowlist \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"userId": "user123"}'

# Enable for all users
curl -X POST https://api.aura.studio/api/featureflags/new_video_editor/enable \
  -H "Authorization: Bearer $TOKEN"
```

## Release Communication

### Internal Communication

**Channels**:
- Slack: #releases
- Email: engineering@aura.studio
- Wiki: Release notes page

**Template**:
```
🚀 Release v1.2.3 - Production Deployment

Status: ✅ Completed
Environment: Production
Deployment Time: 2024-11-10 14:30 UTC
Duration: 12 minutes

📦 What's New:
- Feature 1: Description
- Feature 2: Description
- Bug fix 1: Description

🔗 Links:
- Release Notes: https://github.com/aura/aura/releases/tag/v1.2.3
- Changelog: https://github.com/aura/aura/blob/main/CHANGELOG.md
- Documentation: https://github.com/Coffee285/aura-video-studio/blob/main/docs/releases/v1.2.3

📊 Metrics:
- Tests: 1,234 passed
- Code Coverage: 85%
- Deployment Success Rate: 100%

👏 Contributors: @user1, @user2, @user3
```

### External Communication

**Blog Post**:
```markdown
# Aura v1.2.3 - Enhanced Video Processing

We're excited to announce Aura v1.2.3, bringing significant improvements to video processing performance and new collaboration features.

## Highlights

### 40% Faster Video Encoding
We've optimized our video encoding pipeline...

### Real-time Collaboration
Work together with your team in real-time...

## Get Started

Update to v1.2.3 today:
```bash
docker pull aura/aura-api:v1.2.3
```

[Full release notes](https://github.com/aura/aura/releases/tag/v1.2.3)
```

## Rollback Procedure

If issues are discovered post-release:

```bash
# 1. Assess impact
./deploy/validate-deployment.sh production

# 2. Execute rollback
./deploy/rollback.sh production "Rollback v1.2.3 due to [ISSUE]"

# 3. Verify rollback
./deploy/validate-deployment.sh production

# 4. Communicate
# Send rollback notification to stakeholders

# 5. Create hotfix
# Address issue and release hotfix version
```

## Metrics and KPIs

### Release Metrics

Track the following metrics:

- **Lead Time**: Time from commit to production
  - Target: < 24 hours
  
- **Deployment Frequency**: How often we deploy
  - Target: 2x per month
  
- **Change Failure Rate**: % of deployments causing issues
  - Target: < 5%
  
- **Mean Time to Recovery**: Time to recover from failure
  - Target: < 1 hour

### Monitoring Dashboard

Access release metrics at:
```
https://monitoring.aura.studio/releases
```

## Continuous Improvement

### Retrospectives

After each release:
1. Schedule retrospective meeting
2. Discuss what went well
3. Identify improvements
4. Update documentation
5. Implement changes

### Documentation Updates

Keep these documents updated:
- Release process (this document)
- Deployment guide
- Runbooks
- Troubleshooting guides

---

**Last Updated**: 2024-11-10  
**Document Owner**: Release Manager  
**Review Cycle**: After each release
