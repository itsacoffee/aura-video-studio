# Test Execution Summary

Quick reference for test execution commands and expected outcomes.

## Test Categories

### 1. Unit Tests

**Backend:**
```bash
dotnet test Aura.Tests/Aura.Tests.csproj --filter "Category!=Integration&Category!=E2E&Category!=Performance&Category!=Load"
```
- **Expected Duration**: 3-5 minutes
- **Test Count**: ~2,450 tests
- **Coverage Target**: 80%

**Frontend:**
```bash
cd Aura.Web && npm test
```
- **Expected Duration**: 1-2 minutes
- **Test Count**: Varies by module
- **Coverage Target**: 80%

### 2. Integration Tests

**Backend:**
```bash
dotnet test Aura.Tests/Aura.Tests.csproj --filter "Category=Integration"
```
- **Expected Duration**: 10-15 minutes
- **Test Count**: ~350 tests
- **Uses**: Test containers, in-memory databases

### 3. E2E Tests

**Playwright:**
```bash
cd Aura.Web && npm run playwright
```
- **Expected Duration**: 20-30 minutes
- **Test Count**: 35+ scenarios
- **Browsers**: Chromium (default), Firefox, WebKit (optional)

### 4. Performance Tests

**Load Tests:**
```bash
dotnet test Aura.Tests/Aura.Tests.csproj --filter "Category=Performance|Category=Load"
```
- **Expected Duration**: 10-20 minutes per suite
- **Note**: Usually run manually or scheduled
- **Types**: Load, Stress, Spike, Endurance

## Complete Test Suite

**Run Everything:**
```bash
# Option 1: Use helper script
./scripts/test/run-tests-with-coverage.sh

# Option 2: Manual execution
dotnet test Aura.Tests/Aura.Tests.csproj --collect:"XPlat Code Coverage"
cd Aura.Web && npm run test:coverage && npm run playwright
```

**Expected Total Duration**: 40-50 minutes (when parallelized)

## Coverage Reports

**Generate All Reports:**
```bash
./scripts/test/coverage-analysis.sh
```

**View Reports:**
- Backend: `TestResults/coverage-report/index.html`
- Frontend: `Aura.Web/coverage/index.html`
- Dashboard: `TestResults/dashboard/index.html`

## Performance Benchmarks

**Run Benchmarks:**
```bash
./scripts/test/performance-benchmark.sh
```

**View Results:**
- Report: `TestResults/benchmarks/BENCHMARK_REPORT.md`
- Baseline: `TestResults/benchmarks/baseline.json`

## CI/CD Execution

**Workflow**: `.github/workflows/comprehensive-test-suite.yml`

**Jobs:**
1. `unit-tests-backend` (5-7 min)
2. `unit-tests-frontend` (2-3 min)
3. `integration-tests` (10-15 min)
4. `e2e-tests` (20-30 min)
5. `performance-tests` (scheduled only)
6. `test-report` (artifact generation)
7. `quality-gate` (verification)

**Total CI Duration**: ~40-50 minutes (parallelized)

## Quality Gates

### Required for Merge
- ✅ All unit tests pass
- ✅ All integration tests pass
- ✅ All E2E tests pass
- ✅ Coverage ≥ 80%
- ✅ No security vulnerabilities

### Warnings
- ⚠️ Coverage 70-80%
- ⚠️ Flaky test rate > 2%
- ⚠️ Performance regression > 10%

### Failures
- ❌ Coverage < 70%
- ❌ Any critical test failure
- ❌ Security vulnerability found

## Troubleshooting

### Tests Failing
```bash
# Check for updates
git pull
npm install
dotnet restore

# Clean build
dotnet clean && dotnet build
cd Aura.Web && npm run build

# Run tests again
./scripts/test/run-tests-with-coverage.sh
```

### Coverage Issues
```bash
# Clean coverage cache
rm -rf TestResults/
rm -rf Aura.Web/coverage/

# Regenerate
./scripts/test/coverage-analysis.sh
```

### E2E Issues
```bash
# Update browsers
cd Aura.Web && npx playwright install --with-deps

# Run in debug mode
npx playwright test --debug

# Check for hanging processes
pkill -f playwright
```

## Test Execution Checklist

### Before Running Tests
- [ ] Latest code pulled
- [ ] Dependencies installed
- [ ] Build successful
- [ ] No linting errors

### During Test Execution
- [ ] Monitor for failures
- [ ] Check output for warnings
- [ ] Verify parallel execution working
- [ ] Watch for flaky tests

### After Test Execution
- [ ] Review test results
- [ ] Check coverage reports
- [ ] Address any failures
- [ ] Update baselines if needed

## Performance Characteristics

### Resource Usage
- **CPU**: Utilizes multiple cores (parallel execution)
- **Memory**: ~2-4 GB peak usage
- **Disk**: ~1 GB for test results and coverage
- **Network**: Minimal (most tests mocked)

### Optimization Tips
- Run unit tests frequently (fast feedback)
- Run integration tests before pushing
- Run E2E tests before creating PR
- Run performance tests before release

---

**Quick Reference:**
- Daily: `npm test && dotnet test --filter "Category!=E2E&Category!=Performance"`
- Before PR: `./scripts/test/run-tests-with-coverage.sh`
- Before Release: Full suite + performance tests
