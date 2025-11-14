#!/bin/bash
# Smoke test script for Aura Video Studio startup sanity checks
#
# This script performs critical sanity checks to ensure the application starts cleanly:
# - Builds the solution with dotnet build
# - Runs core unit/integration tests
# - Starts Aura.Api in background
# - Probes health and capabilities endpoints
# - Monitors logs for exceptions over 30 seconds
# - Returns non-zero exit code on failure
#
# Usage:
#   ./start_and_probe.sh [options]
#
# Options:
#   --skip-build       Skip the build step
#   --skip-tests       Skip running tests
#   --test-timeout N   Timeout for tests in seconds (default: 120)
#   --probe-timeout N  Timeout for monitoring in seconds (default: 30)
#   -h, --help         Show this help message

set -e

# Default values
SKIP_BUILD=0
SKIP_TESTS=0
TEST_TIMEOUT=120
PROBE_TIMEOUT=30
API_BASE="http://127.0.0.1:5005"
API_PORT=5005

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Output functions
success() { echo -e "${GREEN}✓ $1${NC}"; }
info() { echo -e "${CYAN}→ $1${NC}"; }
warning() { echo -e "${YELLOW}⚠ $1${NC}"; }
error() { echo -e "${RED}✗ $1${NC}"; }

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --skip-build)
      SKIP_BUILD=1
      shift
      ;;
    --skip-tests)
      SKIP_TESTS=1
      shift
      ;;
    --test-timeout)
      TEST_TIMEOUT="$2"
      shift 2
      ;;
    --probe-timeout)
      PROBE_TIMEOUT="$2"
      shift 2
      ;;
    -h | --help)
      echo "Usage: $0 [options]"
      echo ""
      echo "Options:"
      echo "  --skip-build       Skip the build step"
      echo "  --skip-tests       Skip running tests"
      echo "  --test-timeout N   Timeout for tests in seconds (default: 120)"
      echo "  --probe-timeout N  Timeout for monitoring in seconds (default: 30)"
      echo "  -h, --help         Show this help message"
      exit 0
      ;;
    *)
      error "Unknown option: $1"
      exit 1
      ;;
  esac
done

# Get repository root (2 levels up from scripts/smoke)
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

info "╔═══════════════════════════════════════════════════════╗"
info "║   Aura Video Studio - Startup Smoke Check            ║"
info "╚═══════════════════════════════════════════════════════╝"
echo ""

# Change to repository root
cd "$REPO_ROOT"

# Cleanup function
cleanup() {
  if [ -n "$API_PID" ] && kill -0 "$API_PID" 2>/dev/null; then
    info "Stopping Aura.Api (PID: $API_PID)..."
    kill "$API_PID" 2>/dev/null || true
    sleep 2
    # Force kill if still running
    if kill -0 "$API_PID" 2>/dev/null; then
      warning "Forcing termination of API process..."
      kill -9 "$API_PID" 2>/dev/null || true
    fi
  fi
}

# Register cleanup on exit
trap cleanup EXIT INT TERM

# Step 1: Build the solution
if [ $SKIP_BUILD -eq 0 ]; then
  info "[1/5] Building solution..."
  # Build only core projects to avoid Windows-only Aura.App issues on Linux
  BUILD_PROJECTS=(
    "Aura.Core/Aura.Core.csproj"
    "Aura.Providers/Aura.Providers.csproj"
    "Aura.Api/Aura.Api.csproj"
    "Aura.Tests/Aura.Tests.csproj"
  )

  for project in "${BUILD_PROJECTS[@]}"; do
    if ! dotnet build "$project" --nologo >/tmp/smoke-build.log 2>&1; then
      error "Build failed for $project!"
      cat /tmp/smoke-build.log
      exit 1
    fi
  done
  success "Build completed successfully"
else
  info "[1/5] Skipping build (--skip-build)"
fi

# Step 2: Run core tests
if [ $SKIP_TESTS -eq 0 ]; then
  info "[2/5] Running core tests (Aura.Tests)..."
  if ! timeout "$TEST_TIMEOUT" dotnet test Aura.Tests/Aura.Tests.csproj --no-build --nologo --verbosity quiet >/tmp/smoke-test.log 2>&1; then
    error "Tests failed!"
    cat /tmp/smoke-test.log
    exit 1
  fi
  # Parse test summary
  passed=$(grep -oP "Passed!\s+-\s+Failed:\s+\d+,\s+Passed:\s+\K\d+" /tmp/smoke-test.log 2>/dev/null || echo "")
  if [ -n "$passed" ]; then
    success "Tests passed: $passed tests"
  else
    success "Tests completed"
  fi
else
  info "[2/5] Skipping tests (--skip-tests)"
fi

# Step 3: Start Aura.Api in background
info "[3/5] Starting Aura.Api in background..."

# Check if port is already in use
if netstat -tuln 2>/dev/null | grep -q ":$API_PORT " || ss -tuln 2>/dev/null | grep -q ":$API_PORT "; then
  warning "Port $API_PORT is already in use. Attempting to continue..."
fi

# Create logs directory if it doesn't exist
mkdir -p logs

# Start API in background
dotnet run --project Aura.Api/Aura.Api.csproj --no-build >logs/smoke-stdout.log 2>logs/smoke-stderr.log &
API_PID=$!

success "Aura.Api started (PID: $API_PID)"

# Wait for API to be ready (max 15 seconds)
info "Waiting for API to become ready..."
MAX_WAIT=15
WAITED=0
API_READY=0

while [ $WAITED -lt $MAX_WAIT ]; do
  sleep 1
  WAITED=$((WAITED + 1))

  if curl -s -f "$API_BASE/api/healthz" >/dev/null 2>&1; then
    RESPONSE=$(curl -s "$API_BASE/api/healthz" 2>/dev/null)
    if echo "$RESPONSE" | grep -q '"status":"healthy"'; then
      API_READY=1
      break
    fi
  fi
done

if [ $API_READY -eq 0 ]; then
  error "API did not become ready within $MAX_WAIT seconds"
  exit 1
fi

success "API is ready after $WAITED seconds"

# Step 4: Probe critical endpoints
info "[4/5] Probing critical endpoints..."

# Probe 1: Health check
if ! HEALTH_RESPONSE=$(curl -s -f "$API_BASE/api/healthz" 2>&1); then
  error "GET /api/healthz - FAILED"
  error "  $HEALTH_RESPONSE"
  exit 1
fi

if echo "$HEALTH_RESPONSE" | grep -q '"status":"healthy"'; then
  success "GET /api/healthz - OK"
  TIMESTAMP=$(echo "$HEALTH_RESPONSE" | grep -oP '"timestamp":"[^"]*"' | cut -d'"' -f4)
  if [ -n "$TIMESTAMP" ]; then
    info "  Server time: $TIMESTAMP"
  fi
else
  error "Health check returned unexpected status"
  exit 1
fi

# Probe 2: Capabilities
if ! CAPABILITIES_RESPONSE=$(curl -s -f "$API_BASE/api/capabilities" 2>&1); then
  error "GET /api/capabilities - FAILED"
  error "  $CAPABILITIES_RESPONSE"
  exit 1
fi

success "GET /api/capabilities - OK"
TIER=$(echo "$CAPABILITIES_RESPONSE" | grep -oP '"tier":"[^"]*"' | cut -d'"' -f4)
if [ -n "$TIER" ]; then
  info "  Hardware Tier: $TIER"
fi
CPU_CORES=$(echo "$CAPABILITIES_RESPONSE" | grep -oP '"cores":\d+' | head -1 | grep -oP '\d+')
if [ -n "$CPU_CORES" ]; then
  info "  CPU Cores: $CPU_CORES"
fi

# Probe 3: Queue (jobs/artifacts endpoint)
if ! QUEUE_RESPONSE=$(curl -s -f "$API_BASE/api/queue" 2>&1); then
  error "GET /api/queue - FAILED"
  error "  $QUEUE_RESPONSE"
  exit 1
fi

success "GET /api/queue - OK"
JOBS_COUNT=$(echo "$QUEUE_RESPONSE" | grep -oP '"jobs":\[\]' | wc -l)
if [ -n "$JOBS_COUNT" ]; then
  info "  Jobs in queue: 0"
fi

# Step 5: Monitor logs for exceptions
info "[5/5] Monitoring logs for $PROBE_TIMEOUT seconds..."

# Find the most recent log file
LOG_FILE=$(ls -t logs/aura-api-*.log 2>/dev/null | head -1)

if [ -z "$LOG_FILE" ]; then
  # Fallback to smoke-stdout.log if no dated log file exists
  if [ -f "logs/smoke-stdout.log" ]; then
    LOG_FILE="logs/smoke-stdout.log"
    info "Using smoke-stdout.log for monitoring"
  else
    warning "No log file found - waiting for log creation..."
    sleep 2
    LOG_FILE=$(ls -t logs/aura-api-*.log 2>/dev/null | head -1)
    if [ -z "$LOG_FILE" ] && [ -f "logs/smoke-stdout.log" ]; then
      LOG_FILE="logs/smoke-stdout.log"
    fi
  fi
fi

INITIAL_LINE_COUNT=0
if [ -n "$LOG_FILE" ] && [ -f "$LOG_FILE" ]; then
  INITIAL_LINE_COUNT=$(wc -l <"$LOG_FILE")
  info "Monitoring log file: $(basename "$LOG_FILE") (current lines: $INITIAL_LINE_COUNT)"
fi

# Monitor for specified duration
MONITOR_START=$(date +%s)
EXCEPTION_COUNT=0
ERROR_COUNT=0
declare -a CORRELATION_IDS

while true; do
  CURRENT_TIME=$(date +%s)
  ELAPSED=$((CURRENT_TIME - MONITOR_START))

  if [ $ELAPSED -ge $PROBE_TIMEOUT ]; then
    break
  fi

  sleep 2

  if [ -n "$LOG_FILE" ] && [ -f "$LOG_FILE" ]; then
    CURRENT_LINE_COUNT=$(wc -l <"$LOG_FILE")
    if [ $CURRENT_LINE_COUNT -gt $INITIAL_LINE_COUNT ]; then
      # Read new lines
      NEW_LINES=$(tail -n +$((INITIAL_LINE_COUNT + 1)) "$LOG_FILE")

      # Check for exceptions or errors
      while IFS= read -r line; do
        if echo "$line" | grep -qE "\[ERR\]|\[FTL\]|Exception|Error:"; then
          if echo "$line" | grep -q "Exception"; then
            EXCEPTION_COUNT=$((EXCEPTION_COUNT + 1))
            # Truncate line to 120 characters
            SHORT_LINE=$(echo "$line" | cut -c1-120)
            warning "Exception found: $SHORT_LINE"
          else
            ERROR_COUNT=$((ERROR_COUNT + 1))
          fi
        fi

        # Extract correlation IDs (32 hex characters in brackets)
        CORR_ID=$(echo "$line" | grep -oP '\[([a-f0-9]{32})\]' | tr -d '[]')
        if [ -n "$CORR_ID" ]; then
          # Check if already in array
          if [[ ! " ${CORRELATION_IDS[@]} " =~ " ${CORR_ID} " ]]; then
            CORRELATION_IDS+=("$CORR_ID")
          fi
        fi
      done <<<"$NEW_LINES"

      INITIAL_LINE_COUNT=$CURRENT_LINE_COUNT
    fi
  fi
done

success "Monitoring complete"
info "  Duration: $PROBE_TIMEOUT seconds"
info "  Exceptions found: $EXCEPTION_COUNT"
info "  Errors found: $ERROR_COUNT"

if [ ${#CORRELATION_IDS[@]} -gt 0 ]; then
  info "  Correlation IDs seen: ${#CORRELATION_IDS[@]}"
  for i in "${CORRELATION_IDS[@]:0:3}"; do
    info "    - $i"
  done
fi

# Determine final status
EXIT_CODE=0
if [ $EXCEPTION_COUNT -gt 0 ]; then
  error "Found $EXCEPTION_COUNT exception(s) in logs - test FAILED"
  EXIT_CODE=1
elif [ $ERROR_COUNT -gt 5 ]; then
  warning "Found $ERROR_COUNT error(s) in logs - may indicate issues"
else
  success "No critical exceptions found"
fi

echo ""
info "╔═══════════════════════════════════════════════════════╗"
if [ $EXIT_CODE -eq 0 ]; then
  success "║   Smoke test PASSED - Application is healthy         ║"
else
  error "║   Smoke test FAILED - Check logs for details         ║"
fi
info "╚═══════════════════════════════════════════════════════╝"

exit $EXIT_CODE
