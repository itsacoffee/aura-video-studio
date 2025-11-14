#!/bin/bash
# Local test execution script for Aura Video Studio
# Runs the same tests as CI locally

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "🧪 Aura Video Studio - Local Test Runner"
echo "========================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Parse command line arguments
RUN_DOTNET=true
RUN_FRONTEND=true
RUN_E2E=false
COVERAGE=true

while [[ $# -gt 0 ]]; do
  case $1 in
    --dotnet-only)
      RUN_FRONTEND=false
      RUN_E2E=false
      shift
      ;;
    --frontend-only)
      RUN_DOTNET=false
      RUN_E2E=false
      shift
      ;;
    --e2e)
      RUN_E2E=true
      shift
      ;;
    --no-coverage)
      COVERAGE=false
      shift
      ;;
    --help)
      echo "Usage: $0 [OPTIONS]"
      echo ""
      echo "Options:"
      echo "  --dotnet-only     Run only .NET tests"
      echo "  --frontend-only   Run only frontend tests"
      echo "  --e2e             Include E2E tests (Playwright)"
      echo "  --no-coverage     Skip coverage collection"
      echo "  --help            Show this help message"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      echo "Use --help for usage information"
      exit 1
      ;;
  esac
done

# .NET Tests
if [ "$RUN_DOTNET" = true ]; then
  echo -e "${YELLOW}► Running .NET Tests...${NC}"
  cd "$REPO_ROOT"

  if [ "$COVERAGE" = true ]; then
    echo "  With coverage collection..."
    dotnet test Aura.Tests/Aura.Tests.csproj \
      --configuration Release \
      --logger "console;verbosity=normal" \
      --collect:"XPlat Code Coverage" \
      --results-directory ./TestResults \
      -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover,cobertura

    # Generate coverage report
    if command -v reportgenerator &>/dev/null; then
      echo "  Generating coverage report..."
      reportgenerator \
        -reports:"TestResults/**/coverage.cobertura.xml" \
        -targetdir:"TestResults/CoverageReport" \
        -reporttypes:"Html;TextSummary"

      echo ""
      echo "  Coverage Summary:"
      cat TestResults/CoverageReport/Summary.txt 2>/dev/null || echo "  No summary available"
      echo ""
      echo -e "${GREEN}  ✓ Coverage report: TestResults/CoverageReport/index.html${NC}"
    else
      echo -e "${YELLOW}  ⚠ ReportGenerator not installed. Skipping coverage report.${NC}"
      echo "    Install with: dotnet tool install --global dotnet-reportgenerator-globaltool"
    fi
  else
    dotnet test Aura.Tests/Aura.Tests.csproj \
      --configuration Release \
      --logger "console;verbosity=normal"
  fi

  echo -e "${GREEN}✓ .NET tests completed${NC}"
  echo ""
fi

# Frontend Tests
if [ "$RUN_FRONTEND" = true ]; then
  echo -e "${YELLOW}► Running Frontend Tests...${NC}"
  cd "$REPO_ROOT/Aura.Web"

  if [ "$COVERAGE" = true ]; then
    npm run test:coverage
    echo -e "${GREEN}  ✓ Coverage report: Aura.Web/coverage/index.html${NC}"
  else
    npm run test
  fi

  echo -e "${GREEN}✓ Frontend tests completed${NC}"
  echo ""
fi

# E2E Tests
if [ "$RUN_E2E" = true ]; then
  echo -e "${YELLOW}► Running E2E Tests (Playwright)...${NC}"
  cd "$REPO_ROOT/Aura.Web"

  # Check if Playwright browsers are installed
  if ! npx playwright --version &>/dev/null; then
    echo -e "${YELLOW}  Installing Playwright browsers...${NC}"
    npx playwright install --with-deps chromium
  fi

  npm run playwright

  echo -e "${GREEN}✓ E2E tests completed${NC}"
  echo -e "${GREEN}  ✓ Report: Aura.Web/playwright-report/index.html${NC}"
  echo ""
fi

# Summary
echo "========================================"
echo -e "${GREEN}✓ All tests completed successfully!${NC}"
echo ""
echo "Next steps:"
echo "  - View coverage reports in TestResults/CoverageReport/"
echo "  - View Playwright report: Aura.Web/playwright-report/"
echo "  - Run specific tests: npm test -- <pattern> (frontend)"
echo "  - Run specific tests: dotnet test --filter <filter> (backend)"
