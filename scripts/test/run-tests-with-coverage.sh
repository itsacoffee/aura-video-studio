#!/bin/bash
# Run all tests with coverage reporting
# Usage: ./scripts/test/run-tests-with-coverage.sh [--parallel]

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
PARALLEL=${1:-}
COVERAGE_THRESHOLD=80
BACKEND_DIR="Aura.Tests"
FRONTEND_DIR="Aura.Web"
REPORT_DIR="TestResults"

echo "======================================"
echo "Running Comprehensive Test Suite"
echo "Coverage Threshold: ${COVERAGE_THRESHOLD}%"
echo "======================================"

# Clean previous results
echo -e "\n${YELLOW}Cleaning previous test results...${NC}"
rm -rf "${BACKEND_DIR}/TestResults" 2>/dev/null || true
rm -rf "${FRONTEND_DIR}/coverage" 2>/dev/null || true
rm -rf "${REPORT_DIR}" 2>/dev/null || true
mkdir -p "${REPORT_DIR}"

# Backend Tests
echo -e "\n${YELLOW}Running .NET Backend Tests...${NC}"
cd "${BACKEND_DIR}" || exit 1

if [ "$PARALLEL" == "--parallel" ]; then
  echo "Running tests in parallel mode..."
  dotnet test \
    --configuration Release \
    --settings .runsettings \
    --logger "trx;LogFileName=backend-tests.trx" \
    --logger "html;LogFileName=backend-tests.html" \
    --collect:"XPlat Code Coverage" \
    --results-directory TestResults \
    -- NUnit.NumberOfTestWorkers=auto
else
  dotnet test \
    --configuration Release \
    --settings .runsettings \
    --logger "trx;LogFileName=backend-tests.trx" \
    --logger "html;LogFileName=backend-tests.html" \
    --collect:"XPlat Code Coverage" \
    --results-directory TestResults
fi

BACKEND_EXIT_CODE=$?
cd ..

if [ $BACKEND_EXIT_CODE -ne 0 ]; then
  echo -e "${RED}Backend tests failed!${NC}"
else
  echo -e "${GREEN}Backend tests passed!${NC}"
fi

# Generate backend coverage report
echo -e "\n${YELLOW}Generating backend coverage report...${NC}"
if command -v reportgenerator &>/dev/null; then
  reportgenerator \
    -reports:"${BACKEND_DIR}/TestResults/**/coverage.cobertura.xml" \
    -targetdir:"${REPORT_DIR}/backend-coverage" \
    -reporttypes:"Html;Cobertura;JsonSummary;Badges" \
    -verbosity:Info

  # Extract coverage percentage
  if [ -f "${REPORT_DIR}/backend-coverage/Summary.json" ]; then
    BACKEND_COVERAGE=$(jq -r '.summary.linecoverage' "${REPORT_DIR}/backend-coverage/Summary.json")
    echo -e "${GREEN}Backend Coverage: ${BACKEND_COVERAGE}%${NC}"
  fi
else
  echo -e "${YELLOW}ReportGenerator not found. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool${NC}"
fi

# Frontend Tests
echo -e "\n${YELLOW}Running Frontend Tests...${NC}"
cd "${FRONTEND_DIR}" || exit 1

npm run test:coverage:ci

FRONTEND_EXIT_CODE=$?
cd ..

if [ $FRONTEND_EXIT_CODE -ne 0 ]; then
  echo -e "${RED}Frontend tests failed!${NC}"
else
  echo -e "${GREEN}Frontend tests passed!${NC}"
fi

# Extract frontend coverage
if [ -f "${FRONTEND_DIR}/coverage/coverage-summary.json" ]; then
  FRONTEND_COVERAGE=$(jq -r '.total.lines.pct' "${FRONTEND_DIR}/coverage/coverage-summary.json")
  echo -e "${GREEN}Frontend Coverage: ${FRONTEND_COVERAGE}%${NC}"
fi

# Copy frontend coverage to report directory
cp -r "${FRONTEND_DIR}/coverage" "${REPORT_DIR}/frontend-coverage" 2>/dev/null || true

# Generate combined summary
echo -e "\n======================================"
echo -e "Test Summary"
echo -e "======================================"
echo -e "Backend Tests: $([ $BACKEND_EXIT_CODE -eq 0 ] && echo -e "${GREEN}PASSED${NC}" || echo -e "${RED}FAILED${NC}")"
echo -e "Frontend Tests: $([ $FRONTEND_EXIT_CODE -eq 0 ] && echo -e "${GREEN}PASSED${NC}" || echo -e "${RED}FAILED${NC}")"

if [ -n "${BACKEND_COVERAGE:-}" ]; then
  echo -e "Backend Coverage: ${BACKEND_COVERAGE}%"
fi

if [ -n "${FRONTEND_COVERAGE:-}" ]; then
  echo -e "Frontend Coverage: ${FRONTEND_COVERAGE}%"
fi

echo -e "\nReports available at:"
echo -e "  Backend: ${REPORT_DIR}/backend-coverage/index.html"
echo -e "  Frontend: ${REPORT_DIR}/frontend-coverage/index.html"

# Exit with error if any test suite failed
if [ $BACKEND_EXIT_CODE -ne 0 ] || [ $FRONTEND_EXIT_CODE -ne 0 ]; then
  exit 1
fi

exit 0
