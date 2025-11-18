# CI/CD Guide

Continuous Integration and Deployment workflows for Aura Video Studio.

## Overview

Aura Video Studio uses GitHub Actions for CI/CD with multiple workflows:

- **Build Validation** - Validates builds on every PR
- **Unified CI** - Comprehensive testing and quality checks
- **No Placeholders** - Enforces zero-placeholder policy
- **Platform-Specific Builds** - Windows/Linux/macOS builds

## Workflows

### Build Validation (`build-validation.yml`)

Runs on every pull request and push to main/develop.

**Jobs:**

1. **Windows Build Test** (windows-latest)
   - Setup Node.js 20.x from .nvmrc
   - Run `npm ci` (clean install)
   - Run `npm run build`
   - Verify build artifacts exist
   - Run `npm test`

2. **.NET Build Test** (windows-latest)
   - Setup .NET 8 SDK
   - Run `dotnet restore`
   - Run `dotnet build -c Release` (warnings as errors)
   - Run `dotnet test`

3. **Lint and Type Check** (ubuntu-latest)
   - Run `npm run lint` (zero warnings enforced)
   - Run `npm run typecheck`
   - Run `npm run format:check`

4. **Placeholder Scan** (ubuntu-latest)
   - Run `node scripts/audit/find-placeholders.js`
   - Fail if any TODO/FIXME/HACK found

5. **Environment Validation** (ubuntu-latest)
   - Validate Node.js version consistency
   - Check package.json engines field
   - Verify .nvmrc matches package.json

**Required for merge**: All 5 jobs must pass.

### Unified CI (`ci-unified.yml`)

Comprehensive testing pipeline:

**Jobs:**

1. **.NET Build & Test** (ubuntu-latest)
   - Build all Linux-compatible projects
   - Run unit and integration tests
   - Collect code coverage
   - Upload coverage artifacts

2. **Frontend Build, Lint & Test** (ubuntu-latest)
   - Install dependencies
   - Lint with zero warnings
   - Type check with TypeScript strict mode
   - Run Vitest tests with coverage
   - Build production bundle
   - Upload coverage and build artifacts

3. **E2E Tests** (ubuntu-latest)
   - Install Playwright with chromium
   - Start backend API
   - Run E2E test suite
   - Generate HTML report with traces
   - Upload test results and videos

**Artifacts Generated:**
- Test results (TRX, JUnit XML)
- Coverage reports (Cobertura XML, HTML)
- Playwright HTML report with traces
- Production build artifacts

### No Placeholders (`no-placeholders.yml`)

Dedicated enforcement of zero-placeholder policy:

**Trigger**: Every pull request

**Action**:
- Scans all source files for TODO/FIXME/HACK/WIP
- Excludes markdown documentation files
- Fails PR if any placeholders found
- Provides clear error messages with file locations

**Purpose**: Ensures all committed code is production-ready.

### Platform-Specific CI

**Windows CI** (`.github/workflows/ci-windows.yml`)
- Build on Windows 11
- Test Windows-specific features (SAPI TTS, etc.)
- Validate Electron installer build

**Linux CI** (`.github/workflows/ci-linux.yml`)
- Build on Ubuntu latest
- Test cross-platform compatibility
- Validate AppImage build

**macOS CI** (if configured)
- Build on macOS latest
- Test Apple Silicon and Intel builds
- Validate DMG installer

## Pre-commit Hooks (Husky)

Local enforcement before code reaches CI.

### Pre-commit Hook

**File**: `.husky/pre-commit`

**Actions**:
1. Run placeholder scanner on staged files
2. Reject commit if placeholders found
3. Run lint-staged for auto-formatting

**Bypass** (not recommended):
```bash
git commit --no-verify -m "message"
```
Note: CI will still catch placeholders.

### Commit Message Hook

**File**: `.husky/commit-msg`

**Actions**:
1. Validate commit message format
2. Reject if contains TODO, WIP, FIXME
3. Ensure professional commit messages

## Quality Gates

All PRs must pass these gates before merge:

### 1. Build Success
- ✅ Frontend builds without errors
- ✅ Backend builds without errors or warnings (Release mode)
- ✅ All projects restore dependencies successfully

### 2. Tests Pass
- ✅ All .NET unit tests pass
- ✅ All .NET integration tests pass
- ✅ All frontend unit tests pass
- ✅ All E2E tests pass (when enabled)

### 3. Code Quality
- ✅ ESLint passes with zero warnings (`--max-warnings 0`)
- ✅ TypeScript type checking passes (strict mode)
- ✅ Prettier formatting is correct
- ✅ No placeholder markers (TODO/FIXME/HACK/WIP)

### 4. Coverage Maintained
- ✅ .NET coverage ≥ 60% (target: 80%+)
- ✅ Frontend coverage ≥ 60% (target: 70%+)
- ✅ No significant coverage regression

### 5. Security
- ✅ No high-severity npm audit vulnerabilities
- ✅ No high-severity NuGet vulnerabilities
- ✅ CodeQL scans pass (if configured)

## Local Pre-commit Checklist

Run these before pushing:

```bash
# 1. Scan for placeholders
node scripts/audit/find-placeholders.js

# 2. Lint frontend
cd Aura.Web
npm run lint
npm run typecheck
npm run format:check

# 3. Test frontend
npm test

# 4. Build frontend
npm run build

# 5. Test backend
cd ..
dotnet test

# 6. Build backend (Release mode)
dotnet build -c Release Aura.sln
```

**Shortcut script**:
```bash
./scripts/check-quality-gates.sh
```

## CI Caching

### NuGet Package Cache

```yaml
- uses: actions/cache@v3
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
```

### npm Cache

```yaml
- uses: actions/cache@v3
  with:
    path: ~/.npm
    key: ${{ runner.os }}-npm-${{ hashFiles('**/package-lock.json') }}
```

### Playwright Browsers

```yaml
- uses: actions/cache@v3
  with:
    path: ~/.cache/ms-playwright
    key: ${{ runner.os }}-playwright-${{ hashFiles('**/package-lock.json') }}
```

## Artifact Management

### Test Results

Artifacts are retained for 30 days:

- **Test Results**: TRX and JUnit XML formats
- **Coverage Reports**: Cobertura XML and HTML
- **E2E Reports**: Playwright HTML with traces and videos
- **Build Artifacts**: Production bundles for download

### Downloading Artifacts

From GitHub Actions UI:
1. Go to Actions tab
2. Select workflow run
3. Scroll to "Artifacts" section
4. Download desired artifact

Via CLI:
```bash
gh run download <run-id> -n <artifact-name>
```

## Debugging CI Failures

### 1. Check Logs

View detailed logs in GitHub Actions UI:
- Click on failed job
- Expand failed step
- Review error messages and stack traces

### 2. Download Artifacts

For test failures:
- Download test results XML
- Download Playwright traces (for E2E failures)
- Download screenshots and videos

### 3. Reproduce Locally

```bash
# Use the same Node.js version
nvm use 20

# Clean install (like CI)
cd Aura.Web
rm -rf node_modules package-lock.json
npm install

# Run the same commands as CI
npm run lint
npm run typecheck
npm test
npm run build
```

### 4. Common Issues

**Lint failures**:
- Run `npm run lint:fix` locally
- Check for TypeScript errors with `npm run typecheck`
- Verify Prettier formatting with `npm run format:check`

**Test failures**:
- Run tests locally: `npm test`
- Check for environment-specific issues
- Ensure test data is not hardcoded to specific values

**Placeholder failures**:
- Search for TODO/FIXME/HACK in code
- Remove or replace with production-ready code
- Create GitHub Issues for future work instead

**Build failures**:
- Check for missing dependencies
- Verify Node.js version matches .nvmrc
- Clear caches and retry

## Environment Variables

CI workflows can access secrets and environment variables:

```yaml
env:
  NODE_ENV: production
  CI: true

steps:
  - name: Run tests
    env:
      API_KEY: ${{ secrets.TEST_API_KEY }}
    run: npm test
```

**Available in CI**:
- `CI=true` (always set by GitHub Actions)
- `GITHUB_TOKEN` (automatic)
- Custom secrets (configured in repository settings)

## Deployment

### Electron App Releases

When a tag is pushed:

```bash
git tag v1.0.0
git push origin v1.0.0
```

**Release workflow**:
1. Build for all platforms (Windows, macOS, Linux)
2. Code signing (if configured)
3. Create GitHub Release
4. Upload installers as release assets
5. Publish release notes

### Manual Release

```bash
# Create release manually
gh release create v1.0.0 \
  --title "Release v1.0.0" \
  --notes "Release notes here" \
  Aura.Desktop/dist/*.exe \
  Aura.Desktop/dist/*.dmg \
  Aura.Desktop/dist/*.AppImage
```

## Monitoring CI Performance

Track CI metrics:
- **Build time trends**: Are builds getting slower?
- **Flaky tests**: Which tests fail intermittently?
- **Cache hit rates**: Are caches effective?
- **Artifact sizes**: Are builds growing?

## Best Practices

### 1. Keep CI Fast
- Use caching effectively
- Run tests in parallel when possible
- Optimize build steps
- Target: <10 minutes for most workflows

### 2. Fail Fast
- Run quick checks first (lint, type check)
- Run expensive operations last (E2E tests)
- Cancel in-progress workflows for outdated commits

### 3. Clear Error Messages
- Provide context in failure messages
- Link to documentation for common issues
- Include relevant logs and traces

### 4. Minimize Flaky Tests
- Make tests deterministic
- Avoid hardcoded timeouts
- Use proper wait strategies in E2E tests
- Retry flaky operations with backoff

### 5. Keep Workflows Maintainable
- Use reusable workflows
- Extract common steps to composite actions
- Document workflow purposes
- Keep workflow files small and focused

## Troubleshooting

### CI Passes but Local Fails

```bash
# Ensure same Node.js version
nvm use 20

# Clean install
rm -rf node_modules package-lock.json
npm ci

# Run with CI env variable
CI=true npm test
```

### Local Passes but CI Fails

- Check for environment-specific code
- Verify all dependencies are in package.json (not global)
- Check for hardcoded paths (use path.join)
- Review CI logs for specific error

### Tests Pass in UI but Fail in CI

- E2E tests may need different timeouts
- Check viewport sizes and responsive behavior
- Verify SSE connections work in CI environment

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Quality Gates](QUALITY_GATES.md)
- [Testing Guide](testing.md)
- [Zero Placeholder Policy](ZERO_PLACEHOLDER_POLICY.md)

---

Last updated: 2025-11-18
