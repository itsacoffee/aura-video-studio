#!/bin/bash
# scripts/validate-production-ready.sh
# Automated validation script for production readiness (PR 40)
# 
# This script executes all test suites and validates production readiness
# based on the comprehensive checklist defined in PRODUCTION_READINESS_CHECKLIST.md

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
WEB_DIR="${REPO_ROOT}/Aura.Web"
API_DIR="${REPO_ROOT}/Aura.Api"
TESTS_DIR="${REPO_ROOT}/Aura.Tests"
E2E_DIR="${REPO_ROOT}/Aura.E2E"

# Colors for output
COLOR_RESET='\033[0m'
COLOR_GREEN='\033[0;32m'
COLOR_RED='\033[0;31m'
COLOR_YELLOW='\033[1;33m'
COLOR_CYAN='\033[0;36m'
COLOR_BLUE='\033[0;34m'

# Test results tracking
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
SKIPPED_TESTS=0

# Helper functions
print_header() {
    echo ""
    echo "========================================"
    echo " $1"
    echo "========================================"
    echo ""
}

print_section() {
    echo ""
    echo -e "${COLOR_BLUE}▶ $1${COLOR_RESET}"
    echo ""
}

print_success() {
    echo -e "${COLOR_GREEN}✓${COLOR_RESET} $1"
    ((PASSED_TESTS++))
    ((TOTAL_TESTS++))
}

print_fail() {
    echo -e "${COLOR_RED}✗${COLOR_RESET} $1"
    ((FAILED_TESTS++))
    ((TOTAL_TESTS++))
}

print_skip() {
    echo -e "${COLOR_YELLOW}⊝${COLOR_RESET} $1"
    ((SKIPPED_TESTS++))
    ((TOTAL_TESTS++))
}

print_info() {
    echo -e "${COLOR_CYAN}→${COLOR_RESET} $1"
}

print_warning() {
    echo -e "${COLOR_YELLOW}⚠${COLOR_RESET} $1"
}

# Start validation
print_header "Aura Video Studio - Production Readiness Validation"
echo "Date: $(date)"
echo "Repository: ${REPO_ROOT}"
echo ""

# PHASE 1: Environment Check
print_section "PHASE 1: Environment Check"

print_info "Checking Node.js version..."
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    print_success "Node.js installed: ${NODE_VERSION}"
else
    print_fail "Node.js not found"
    exit 1
fi

print_info "Checking npm version..."
if command -v npm &> /dev/null; then
    NPM_VERSION=$(npm --version)
    print_success "npm installed: ${NPM_VERSION}"
else
    print_fail "npm not found"
    exit 1
fi

print_info "Checking .NET version..."
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    print_success ".NET installed: ${DOTNET_VERSION}"
else
    print_warning ".NET not found (optional for frontend-only validation)"
fi

# PHASE 2: Dependency Installation
print_section "PHASE 2: Dependency Installation"

print_info "Installing frontend dependencies..."
cd "${WEB_DIR}"
if npm install --silent > /dev/null 2>&1; then
    print_success "Frontend dependencies installed"
else
    print_fail "Failed to install frontend dependencies"
    exit 1
fi

# PHASE 3: Frontend Unit Tests
print_section "PHASE 3: Frontend Unit Tests (Vitest)"

print_info "Running frontend unit tests..."
cd "${WEB_DIR}"
if npm test 2>&1 | tee /tmp/frontend-tests.log; then
    print_success "Frontend unit tests passed"
    # Extract test count from output
    TEST_COUNT=$(grep -oP '\d+ passed' /tmp/frontend-tests.log | head -1 | grep -oP '\d+')
    print_info "Tests passed: ${TEST_COUNT}"
else
    print_fail "Frontend unit tests failed"
    print_warning "Check /tmp/frontend-tests.log for details"
fi

# PHASE 4: Smoke Tests
print_section "PHASE 4: Smoke Tests"

print_info "Running dependency detection smoke tests..."
if npm test -- tests/smoke/dependency-detection.test.ts 2>&1 | tee /tmp/smoke-dependency.log; then
    print_success "Dependency detection smoke tests passed"
else
    print_fail "Dependency detection smoke tests failed"
fi

print_info "Running Quick Demo smoke tests..."
if npm test -- tests/smoke/quick-demo.test.ts 2>&1 | tee /tmp/smoke-quickdemo.log; then
    print_success "Quick Demo smoke tests passed"
else
    print_fail "Quick Demo smoke tests failed"
fi

print_info "Running export pipeline smoke tests..."
if npm test -- tests/smoke/export-pipeline.test.ts 2>&1 | tee /tmp/smoke-export.log; then
    print_success "Export pipeline smoke tests passed"
else
    print_fail "Export pipeline smoke tests failed"
fi

print_info "Running settings smoke tests..."
if npm test -- tests/smoke/settings.test.ts 2>&1 | tee /tmp/smoke-settings.log; then
    print_success "Settings smoke tests passed"
else
    print_fail "Settings smoke tests failed"
fi

# PHASE 5: Integration Tests
print_section "PHASE 5: Integration Tests"

print_info "Running critical paths integration tests..."
if npm test -- tests/integration/critical-paths.test.ts 2>&1 | tee /tmp/integration-tests.log; then
    print_success "Critical paths integration tests passed"
else
    print_fail "Critical paths integration tests failed"
fi

# PHASE 6: Build Verification
print_section "PHASE 6: Build Verification"

print_info "Running production build..."
cd "${WEB_DIR}"
if npm run build 2>&1 | tee /tmp/build.log; then
    print_success "Production build completed"
    
    # Check bundle size
    if [ -d "dist" ]; then
        BUNDLE_SIZE=$(du -sh dist | cut -f1)
        print_info "Bundle size: ${BUNDLE_SIZE}"
        
        # Check if main bundle is gzipped under 2MB
        if [ -f "dist/assets/index.js" ]; then
            MAIN_BUNDLE_SIZE=$(gzip -c dist/assets/index*.js | wc -c)
            MAIN_BUNDLE_SIZE_MB=$((MAIN_BUNDLE_SIZE / 1024 / 1024))
            if [ ${MAIN_BUNDLE_SIZE_MB} -lt 2 ]; then
                print_success "Main bundle size OK: ${MAIN_BUNDLE_SIZE_MB}MB (< 2MB target)"
            else
                print_warning "Main bundle size: ${MAIN_BUNDLE_SIZE_MB}MB (exceeds 2MB target)"
            fi
        fi
    fi
else
    print_fail "Production build failed"
fi

# PHASE 7: Type Checking
print_section "PHASE 7: Type Checking"

print_info "Running TypeScript type check..."
cd "${WEB_DIR}"
if npm run type-check 2>&1 | tee /tmp/typecheck.log; then
    print_success "TypeScript type check passed"
else
    print_fail "TypeScript type check failed"
fi

# PHASE 8: Linting
print_section "PHASE 8: Code Quality (Linting)"

print_info "Running ESLint..."
cd "${WEB_DIR}"
if npm run lint 2>&1 | tee /tmp/eslint.log; then
    print_success "ESLint passed with no errors"
else
    print_warning "ESLint found issues (check /tmp/eslint.log)"
fi

# PHASE 9: Backend Tests (Optional)
print_section "PHASE 9: Backend Tests (Optional)"

if command -v dotnet &> /dev/null; then
    print_info "Running backend unit tests..."
    cd "${REPO_ROOT}"
    if dotnet test --no-build --verbosity quiet 2>&1 | tee /tmp/backend-tests.log; then
        print_success "Backend unit tests passed"
    else
        print_warning "Backend unit tests failed or not available"
    fi
else
    print_skip "Backend tests skipped (.NET not available)"
fi

# PHASE 10: E2E Tests (Optional - requires running server)
print_section "PHASE 10: E2E Tests (Optional)"

print_info "E2E tests require running server - skipped in automated validation"
print_skip "Playwright E2E tests (run manually with: npm run playwright)"

# Summary
print_header "Validation Summary"

echo "Total Checks: ${TOTAL_TESTS}"
echo -e "${COLOR_GREEN}Passed: ${PASSED_TESTS}${COLOR_RESET}"
echo -e "${COLOR_RED}Failed: ${FAILED_TESTS}${COLOR_RESET}"
echo -e "${COLOR_YELLOW}Skipped: ${SKIPPED_TESTS}${COLOR_RESET}"
echo ""

# Calculate success rate
if [ ${TOTAL_TESTS} -gt 0 ]; then
    SUCCESS_RATE=$((PASSED_TESTS * 100 / TOTAL_TESTS))
    echo "Success Rate: ${SUCCESS_RATE}%"
fi

# Generate report
print_section "Generating Report"

REPORT_FILE="${REPO_ROOT}/validation-report-$(date +%Y%m%d-%H%M%S).txt"
cat > "${REPORT_FILE}" <<EOF
Aura Video Studio - Production Readiness Validation Report
=========================================================

Date: $(date)
Node.js: ${NODE_VERSION}
npm: ${NPM_VERSION}

Test Results:
- Total Checks: ${TOTAL_TESTS}
- Passed: ${PASSED_TESTS}
- Failed: ${FAILED_TESTS}
- Skipped: ${SKIPPED_TESTS}
- Success Rate: ${SUCCESS_RATE}%

Validation Phases:
1. Environment Check: ✓
2. Dependency Installation: ✓
3. Frontend Unit Tests: $([ ${FAILED_TESTS} -eq 0 ] && echo "✓" || echo "✗")
4. Smoke Tests: $([ ${FAILED_TESTS} -eq 0 ] && echo "✓" || echo "✗")
5. Integration Tests: $([ ${FAILED_TESTS} -eq 0 ] && echo "✓" || echo "✗")
6. Build Verification: $([ -d "${WEB_DIR}/dist" ] && echo "✓" || echo "✗")
7. Type Checking: $([ ${FAILED_TESTS} -eq 0 ] && echo "✓" || echo "✗")
8. Code Quality: $([ ${FAILED_TESTS} -eq 0 ] && echo "✓" || echo "✗")
9. Backend Tests: Skipped
10. E2E Tests: Skipped (manual)

Detailed Logs:
- Frontend Tests: /tmp/frontend-tests.log
- Smoke Tests: /tmp/smoke-*.log
- Integration Tests: /tmp/integration-tests.log
- Build Log: /tmp/build.log
- Type Check: /tmp/typecheck.log
- ESLint: /tmp/eslint.log

Recommendations:
$( [ ${FAILED_TESTS} -eq 0 ] && echo "✓ All automated checks passed!" || echo "✗ Some checks failed. Review logs for details." )
$( [ ${SKIPPED_TESTS} -gt 0 ] && echo "⚠ ${SKIPPED_TESTS} checks were skipped. Run manually for complete validation." || echo "" )

Next Steps:
1. Review PRODUCTION_READINESS_CHECKLIST.md
2. Run E2E tests manually: npm run playwright
3. Perform manual testing of critical paths
4. Update docs/TESTING_RESULTS.md with findings
5. Get sign-off from reviewers

EOF

print_info "Report saved to: ${REPORT_FILE}"

# Final status
echo ""
if [ ${FAILED_TESTS} -eq 0 ]; then
    print_header "✓ Production Readiness Validation PASSED"
    exit 0
else
    print_header "✗ Production Readiness Validation FAILED"
    print_warning "Review failed tests and fix issues before production release"
    exit 1
fi
