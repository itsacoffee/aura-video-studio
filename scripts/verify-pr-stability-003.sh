#!/bin/bash
# Verification script for PR-STABILITY-003 Network Resilience implementation

echo "========================================="
echo "PR-STABILITY-003 Verification Script"
echo "Network Resilience & Retry Logic"
echo "========================================="
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Track results
PASSED=0
FAILED=0

# Function to run check
check() {
  local name="$1"
  local command="$2"

  echo -n "Checking: $name... "
  if eval "$command" >/dev/null 2>&1; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
  else
    echo -e "${RED}✗ FAIL${NC}"
    ((FAILED++))
  fi
}

# 1. Check if new files exist
echo "=== File Existence Checks ==="
check "Network Resilience Service" "test -f Aura.Web/src/services/networkResilience.ts"
check "Timeout Configuration" "test -f Aura.Web/src/config/timeouts.ts"
check "Network Resilience Tests" "test -f Aura.Web/src/services/__tests__/networkResilience.test.ts"
check "Timeout Configuration Tests" "test -f Aura.Web/src/config/__tests__/timeouts.test.ts"
check "Configuration Guide" "test -f NETWORK_RESILIENCE_GUIDE.md"
check "Implementation Summary" "test -f PR_STABILITY_003_SUMMARY.md"
echo ""

# 2. Check for placeholders in new files
echo "=== Placeholder Check (New Files Only) ==="
NEW_FILES="Aura.Web/src/services/networkResilience.ts Aura.Web/src/config/timeouts.ts"
PLACEHOLDER_FOUND=0
for file in $NEW_FILES; do
  if grep -qi "TODO\|FIXME\|HACK\|WIP" "$file" 2>/dev/null; then
    echo -e "${RED}✗ Found placeholder in $file${NC}"
    PLACEHOLDER_FOUND=1
    ((FAILED++))
  fi
done
if [ $PLACEHOLDER_FOUND -eq 0 ]; then
  echo -e "${GREEN}✓ No placeholders in new files${NC}"
  ((PASSED++))
fi
echo ""

# 3. Check TypeScript compilation
echo "=== TypeScript Compilation Check ==="
cd Aura.Web
check "TypeScript compilation (new files)" "npx tsc --noEmit src/services/networkResilience.ts src/config/timeouts.ts"
cd ..
echo ""

# 4. Run tests
echo "=== Test Execution ==="
cd Aura.Web
echo "Running Network Resilience tests..."
if npm test -- src/services/__tests__/networkResilience.test.ts --run --reporter=basic 2>&1 | grep -q "16 passed"; then
  echo -e "${GREEN}✓ Network Resilience tests (16/16)${NC}"
  ((PASSED++))
else
  echo -e "${RED}✗ Network Resilience tests failed${NC}"
  ((FAILED++))
fi

echo "Running Timeout Configuration tests..."
if npm test -- src/config/__tests__/timeouts.test.ts --run --reporter=basic 2>&1 | grep -q "19 passed"; then
  echo -e "${GREEN}✓ Timeout Configuration tests (19/19)${NC}"
  ((PASSED++))
else
  echo -e "${RED}✗ Timeout Configuration tests failed${NC}"
  ((FAILED++))
fi

echo "Running API Client tests (regression check)..."
if npm test -- src/services/api/__tests__/ --run --reporter=basic 2>&1 | grep -q "72 passed"; then
  echo -e "${GREEN}✓ API Client tests (72/72)${NC}"
  ((PASSED++))
else
  echo -e "${RED}✗ API Client tests failed${NC}"
  ((FAILED++))
fi
cd ..
echo ""

# 5. Check backend resilience infrastructure
echo "=== Backend Infrastructure Check ==="
check "Resilience Pipeline Factory" "test -f Aura.Core/Resilience/ResiliencePipelineFactory.cs"
check "Resilience Services Extensions" "test -f Aura.Api/Startup/ResilienceServicesExtensions.cs"
check "Circuit Breaker State Manager" "test -f Aura.Core/Resilience/CircuitBreakerStateManager.cs"
echo ""

# 6. Check integration points
echo "=== Integration Check ==="
check "apiClient imports timeoutConfig" "grep -q 'timeoutConfig' Aura.Web/src/services/api/apiClient.ts"
check "apiClient exports networkResilienceService" "grep -q 'networkResilienceService' Aura.Web/src/services/api/apiClient.ts"
check "appStore has isOnline" "grep -q 'isOnline.*navigator.onLine' Aura.Web/src/stores/appStore.ts"
echo ""

# 7. Check documentation
echo "=== Documentation Check ==="
check "Configuration guide has frontend section" "grep -q 'Frontend Resilience' NETWORK_RESILIENCE_GUIDE.md"
check "Configuration guide has backend section" "grep -q 'Backend Resilience' NETWORK_RESILIENCE_GUIDE.md"
check "Configuration guide has examples" "grep -q '\`\`\`typescript' NETWORK_RESILIENCE_GUIDE.md"
check "Summary has test coverage" "grep -q '107' PR_STABILITY_003_SUMMARY.md"
echo ""

# Final summary
echo "========================================="
echo "Verification Summary"
echo "========================================="
echo -e "Passed: ${GREEN}${PASSED}${NC}"
echo -e "Failed: ${RED}${FAILED}${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
  echo -e "${GREEN}✓ All checks passed! Implementation verified.${NC}"
  exit 0
else
  echo -e "${RED}✗ Some checks failed. Please review.${NC}"
  exit 1
fi
