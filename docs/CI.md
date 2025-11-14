# CI/CD Workflows

This document describes the continuous integration and deployment workflows for Aura Video Studio.

## Workflows

### 1. Main CI Workflow (`ci.yml`)
**Trigger**: Push to `main` or `develop`, Pull requests  
**Runner**: `windows-latest`

**Jobs**:
- `build-and-test`: Builds all .NET projects and runs unit tests
- `portable-only-guard`: Enforces portable-only policy (no installers)
- `web-tests`: Runs web frontend tests with Vitest and Playwright

**Artifacts**:
- Test results (TRX format)
- Playwright HTML reports
- Coverage reports (JSON/HTML)

### 2. Windows CI Workflow (`ci-windows.yml`)
**Trigger**: Push to `main`, Pull requests  
**Runner**: `windows-latest`

**Pipeline Steps**:
1. **Setup**: Checkout, .NET 8, Node 20, pnpm
2. **Placeholder Scan**: Fails if TODO/FIXME/FUTURE markers found
3. **Audit**: Runs Windows-specific audit scripts
4. **Build**: Restores and builds .NET solution
5. **Test**: Runs .NET tests with code coverage
6. **Web Tests**: Installs deps, runs Vitest + Playwright
7. **Smoke Test**: Runs PowerShell smoke script to generate demo video
8. **Upload Artifacts**: Smoke outputs, test results, coverage, Playwright reports

**Coverage Thresholds**:
- **.NET**: 60% line coverage (enforced via coverlet)
- **Web**: 70% on tested files (enforced via vitest)

**Artifacts**:
- `windows-smoke-artifacts`: demo.mp4, demo.srt, logs.zip
- `windows-test-results`: TRX files, coverage XML
- `windows-playwright-report`: E2E test reports
- `windows-coverage-reports`: Coverage JSON/HTML/XML

### 3. Linux CI Workflow (`ci-linux.yml`)
**Trigger**: Push to `main`, Pull requests  
**Runner**: `ubuntu-latest`

**Pipeline Steps**: Same as Windows but with:
- Bash smoke script instead of PowerShell
- Linux-compatible Playwright installation
- Different artifact naming (`linux-*`)

### 4. No Placeholders Workflow (`no-placeholders.yml`)
**Trigger**: Pull requests (opened, synchronized, reopened)  
**Runner**: `ubuntu-latest`

**Purpose**: Enforces zero-tolerance policy for placeholder markers:
- `TODO`
- `FIXME`
- `FUTURE IMPLEMENTATION`
- `FUTURE`
- `NEXT STEPS`
- `OPTIONAL ENHANCEMENTS`

If any markers are found, the workflow fails with an error.

## E2E Test Coverage

### .NET E2E Tests
Located in `Aura.E2E/`:
- **SmokeTests.cs**: Free-only and Mixed-mode pipeline tests
- **UnitTest1.cs**: Component integration tests
- **ProviderValidationApiTests.cs**: API integration tests (requires running server)

### Web E2E Tests
Located in `Aura.Web/tests/e2e/`:
- **wizard.spec.ts**: Wizard flow with Free profile
- Navigation and state persistence tests
- Mocked API responses for offline testing

## Smoke Tests

### Purpose
Quick validation that core functionality works end-to-end without external dependencies.

### Windows Smoke Script
**Path**: `scripts/run_quick_generate_demo.ps1`

**Features**:
- Tries API pipeline first
- Falls back to ffmpeg color bars if API unavailable
- Generates: demo.mp4, demo.srt, logs.zip
- Prints absolute paths at completion
- Exit code 0 on success, 1 on failure

**Usage**:
```powershell
pwsh -File .\scripts\run_quick_generate_demo.ps1 -FfmpegPath ".\scripts\ffmpeg\ffmpeg.exe"
```

### Linux Smoke Script
**Path**: `scripts/run_quick_generate_demo.sh`

**Features**: Same as Windows but in Bash

**Usage**:
```bash
chmod +x ./scripts/run_quick_generate_demo.sh
./scripts/run_quick_generate_demo.sh
```

**Environment Variables**:
- `API_BASE`: API endpoint (default: `http://127.0.0.1:5000`)
- `FFMPEG_PATH`: Path to ffmpeg binary (default: `ffmpeg`)
- `SECONDS_DURATION`: Video duration (default: `10`)

## Coverage Enforcement

### Web Coverage (Vitest)
Configured in `Aura.Web/vite.config.ts`:
```typescript
thresholds: {
  lines: 70,
  functions: 70,
  branches: 70,
  statements: 70,
  perFile: true
}
```

Vitest will fail if any tested file is below 70% coverage.

### .NET Coverage (coverlet)
Threshold: 60% line coverage

Checked via `scripts/check_coverage_thresholds.sh`:
- Parses `coverage.cobertura.xml` files
- Extracts line-rate and converts to percentage
- Fails if any project is below 60%

**Manual Check**:
```bash
chmod +x ./scripts/check_coverage_thresholds.sh
./scripts/check_coverage_thresholds.sh
```

## Caching

Both workflows use caching to speed up builds:

### NuGet Cache
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: "8.0.x"
    cache: true
```

### npm Cache
```yaml
- name: Setup Node
  uses: actions/setup-node@v4
  with:
    node-version: '20'
    cache: 'npm'
    cache-dependency-path: Aura.Web/package-lock.json
```

### pnpm Cache
```yaml
- name: Setup pnpm
  uses: pnpm/action-setup@v4
  with:
    version: 8
```

## Artifact Retention

All artifacts are retained for **30 days**:
- Smoke outputs (MP4, SRT, logs)
- Test results (TRX, XML)
- Coverage reports (JSON, HTML, XML)
- Playwright reports (HTML with traces)

## Triggering Workflows

### Automatic Triggers
- **Push to main**: All workflows except `no-placeholders.yml`
- **Pull Request**: All workflows
- **Manual**: `ci.yml` supports `workflow_dispatch`

### Manual Trigger
```bash
# Via GitHub CLI
gh workflow run ci.yml

# Via GitHub UI
Actions → CI → Run workflow
```

## Debugging CI Failures

### 1. Check Artifacts
Download artifacts from failed runs:
```bash
gh run download <run-id>
```

### 2. View Logs
```bash
gh run view <run-id> --log
```

### 3. Reproduce Locally

**Windows**:
```powershell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
cd Aura.Web
npm ci
npm test
npm run playwright
```

**Linux**:
```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
cd Aura.Web
npm ci
npm test
npm run playwright
```

### 4. Run Smoke Test Locally
```bash
# Windows
pwsh -File .\scripts\run_quick_generate_demo.ps1

# Linux
./scripts/run_quick_generate_demo.sh
```

## Best Practices

### Writing Tests
1. **Fast**: Aim for <5s per test
2. **Deterministic**: No random failures
3. **Isolated**: No shared state between tests
4. **Descriptive**: Clear test names and assertions

### Adding CI Steps
1. **Fail Fast**: Put quick checks (placeholder scan) early
2. **Cache Dependencies**: Use setup actions with caching
3. **Upload on Failure**: Use `if: always()` for debugging artifacts
4. **Meaningful Names**: Clear step names for easy log scanning

### Coverage Goals
- **Unit Tests**: Aim for 80%+ on new code
- **Integration Tests**: Cover happy path + error cases
- **E2E Tests**: Focus on user-critical workflows

## Troubleshooting

### "Placeholder markers found" Error
Search for forbidden keywords:
```bash
grep -r "TODO\|FIXME\|FUTURE" --include="*.cs" --include="*.ts" .
```

Remove or implement all placeholders before merging.

### Coverage Below Threshold
Run coverage locally:
```bash
# .NET
dotnet test --collect:"XPlat Code Coverage"
dotnet reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# Web
cd Aura.Web
npm run test:coverage
open coverage/index.html
```

Add tests for uncovered code paths.

### Playwright Timeout
Increase timeout in `playwright.config.ts`:
```typescript
webServer: {
  timeout: 180 * 1000, // 3 minutes
}
```

Or check if dev server is failing to start.

## Related Documentation

- [E2E Test README](../Aura.E2E/README.md)
- [Web Testing Setup](../README.md)
- [Build & Run Guide](developer/BUILD_AND_RUN.md)
