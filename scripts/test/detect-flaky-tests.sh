#!/bin/bash
# Detect flaky tests by running the test suite multiple times
# Usage: ./scripts/test/detect-flaky-tests.sh [iterations]

set -euo pipefail

ITERATIONS=${1:-10}
BACKEND_DIR="Aura.Tests"
FRONTEND_DIR="Aura.Web"
RESULTS_DIR="flaky-test-results"

echo "======================================"
echo "Flaky Test Detection"
echo "Running test suite ${ITERATIONS} times"
echo "======================================"

# Clean previous results
rm -rf "${RESULTS_DIR}" 2>/dev/null || true
mkdir -p "${RESULTS_DIR}/backend"
mkdir -p "${RESULTS_DIR}/frontend"

# Backend flaky test detection
echo -e "\nChecking backend tests for flakiness..."
BACKEND_FAILURES=0
BACKEND_FLAKY_TESTS=()

for i in $(seq 1 $ITERATIONS); do
  echo "Backend iteration $i/$ITERATIONS"

  cd "${BACKEND_DIR}"
  dotnet test --configuration Release --no-build \
    --logger "trx;LogFileName=run-${i}.trx" \
    --results-directory "../${RESULTS_DIR}/backend" \
    >"../${RESULTS_DIR}/backend/run-${i}.log" 2>&1

  EXIT_CODE=$?
  cd ..

  if [ $EXIT_CODE -ne 0 ]; then
    BACKEND_FAILURES=$((BACKEND_FAILURES + 1))
    echo "  ❌ Failed"
  else
    echo "  ✅ Passed"
  fi
done

# Frontend flaky test detection
echo -e "\nChecking frontend tests for flakiness..."
FRONTEND_FAILURES=0
FRONTEND_FLAKY_TESTS=()

for i in $(seq 1 $ITERATIONS); do
  echo "Frontend iteration $i/$ITERATIONS"

  cd "${FRONTEND_DIR}"
  npm test -- --reporter=json --outputFile="../${RESULTS_DIR}/frontend/run-${i}.json" \
    >"../${RESULTS_DIR}/frontend/run-${i}.log" 2>&1

  EXIT_CODE=$?
  cd ..

  if [ $EXIT_CODE -ne 0 ]; then
    FRONTEND_FAILURES=$((FRONTEND_FAILURES + 1))
    echo "  ❌ Failed"
  else
    echo "  ✅ Passed"
  fi
done

# Analyze results
echo -e "\n======================================"
echo "Flaky Test Analysis"
echo "======================================"

echo -e "\nBackend Tests:"
if [ $BACKEND_FAILURES -eq 0 ]; then
  echo "  ✅ No flaky tests detected ($ITERATIONS/$ITERATIONS passed)"
elif [ $BACKEND_FAILURES -eq $ITERATIONS ]; then
  echo "  ❌ All runs failed ($BACKEND_FAILURES/$ITERATIONS) - tests are consistently failing"
else
  echo "  ⚠️  FLAKY TESTS DETECTED ($BACKEND_FAILURES/$ITERATIONS failed)"
  echo "  This indicates intermittent test failures that need investigation"
fi

echo -e "\nFrontend Tests:"
if [ $FRONTEND_FAILURES -eq 0 ]; then
  echo "  ✅ No flaky tests detected ($ITERATIONS/$ITERATIONS passed)"
elif [ $FRONTEND_FAILURES -eq $ITERATIONS ]; then
  echo "  ❌ All runs failed ($FRONTEND_FAILURES/$ITERATIONS) - tests are consistently failing"
else
  echo "  ⚠️  FLAKY TESTS DETECTED ($FRONTEND_FAILURES/$ITERATIONS failed)"
  echo "  This indicates intermittent test failures that need investigation"
fi

# Create summary report
cat >"${RESULTS_DIR}/summary.txt" <<EOF
Flaky Test Detection Summary
====================================
Iterations: ${ITERATIONS}

Backend Results:
- Failures: ${BACKEND_FAILURES}/${ITERATIONS}
- Status: $([ $BACKEND_FAILURES -eq 0 ] && echo "STABLE" || ([ $BACKEND_FAILURES -eq $ITERATIONS ] && echo "CONSISTENTLY FAILING" || echo "FLAKY"))

Frontend Results:
- Failures: ${FRONTEND_FAILURES}/${ITERATIONS}
- Status: $([ $FRONTEND_FAILURES -eq 0 ] && echo "STABLE" || ([ $FRONTEND_FAILURES -eq $ITERATIONS ] && echo "CONSISTENTLY FAILING" || echo "FLAKY"))

Results Location: ${RESULTS_DIR}
EOF

echo -e "\nSummary saved to: ${RESULTS_DIR}/summary.txt"

# Exit with error if flaky tests detected
TOTAL_FLAKY=0
if [ $BACKEND_FAILURES -gt 0 ] && [ $BACKEND_FAILURES -lt $ITERATIONS ]; then
  TOTAL_FLAKY=$((TOTAL_FLAKY + 1))
fi
if [ $FRONTEND_FAILURES -gt 0 ] && [ $FRONTEND_FAILURES -lt $ITERATIONS ]; then
  TOTAL_FLAKY=$((TOTAL_FLAKY + 1))
fi

if [ $TOTAL_FLAKY -gt 0 ]; then
  echo -e "\n⚠️  WARNING: Flaky tests detected. Please investigate and fix."
  exit 1
fi

echo -e "\n✅ No flaky tests detected!"
exit 0
