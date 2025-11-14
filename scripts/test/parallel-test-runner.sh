#!/bin/bash
# Run tests in parallel for faster execution
# Usage: ./scripts/test/parallel-test-runner.sh

set -euo pipefail

echo "======================================"
echo "Parallel Test Execution"
echo "======================================"

# Start backend tests in background
echo "Starting backend tests..."
(
  cd Aura.Tests
  dotnet test \
    --configuration Release \
    --settings .runsettings \
    --logger "trx;LogFileName=backend-parallel.trx" \
    --results-directory TestResults \
    -- NUnit.NumberOfTestWorkers=auto
  echo $? >/tmp/backend_exit_code
) &
BACKEND_PID=$!

# Start frontend unit tests in background
echo "Starting frontend unit tests..."
(
  cd Aura.Web
  npm run test:unit -- --reporter=verbose
  echo $? >/tmp/frontend_unit_exit_code
) &
FRONTEND_UNIT_PID=$!

# Start frontend integration tests in background
echo "Starting frontend integration tests..."
(
  cd Aura.Web
  npm run test:integration -- --reporter=verbose
  echo $? >/tmp/frontend_integration_exit_code
) &
FRONTEND_INTEGRATION_PID=$!

# Start E2E smoke tests in background
echo "Starting E2E smoke tests..."
(
  cd Aura.Web
  npm run test:smoke -- --reporter=verbose
  echo $? >/tmp/frontend_smoke_exit_code
) &
FRONTEND_SMOKE_PID=$!

# Wait for all tests to complete
echo -e "\nWaiting for all test suites to complete..."
wait $BACKEND_PID
wait $FRONTEND_UNIT_PID
wait $FRONTEND_INTEGRATION_PID
wait $FRONTEND_SMOKE_PID

# Collect exit codes
BACKEND_EXIT=$(cat /tmp/backend_exit_code 2>/dev/null || echo 1)
FRONTEND_UNIT_EXIT=$(cat /tmp/frontend_unit_exit_code 2>/dev/null || echo 1)
FRONTEND_INTEGRATION_EXIT=$(cat /tmp/frontend_integration_exit_code 2>/dev/null || echo 1)
FRONTEND_SMOKE_EXIT=$(cat /tmp/frontend_smoke_exit_code 2>/dev/null || echo 1)

# Clean up temp files
rm -f /tmp/backend_exit_code /tmp/frontend_unit_exit_code /tmp/frontend_integration_exit_code /tmp/frontend_smoke_exit_code

# Display results
echo -e "\n======================================"
echo "Test Results Summary"
echo "======================================"
echo "Backend Tests: $([ $BACKEND_EXIT -eq 0 ] && echo '✅ PASSED' || echo '❌ FAILED')"
echo "Frontend Unit Tests: $([ $FRONTEND_UNIT_EXIT -eq 0 ] && echo '✅ PASSED' || echo '❌ FAILED')"
echo "Frontend Integration Tests: $([ $FRONTEND_INTEGRATION_EXIT -eq 0 ] && echo '✅ PASSED' || echo '❌ FAILED')"
echo "Frontend Smoke Tests: $([ $FRONTEND_SMOKE_EXIT -eq 0 ] && echo '✅ PASSED' || echo '❌ FAILED')"

# Exit with error if any suite failed
if [ $BACKEND_EXIT -ne 0 ] || [ $FRONTEND_UNIT_EXIT -ne 0 ] \
  || [ $FRONTEND_INTEGRATION_EXIT -ne 0 ] || [ $FRONTEND_SMOKE_EXIT -ne 0 ]; then
  echo -e "\n❌ One or more test suites failed"
  exit 1
fi

echo -e "\n✅ All test suites passed!"
exit 0
