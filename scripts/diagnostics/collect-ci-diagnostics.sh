#!/bin/bash
# Collect diagnostic bundle for failed CI runs

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
OUTPUT_DIR="${1:-$PROJECT_ROOT/ci-diagnostics}"
CORRELATION_ID="${2:-ci-$(date +%s)}"

echo "========================================="
echo "Collecting CI Diagnostics"
echo "========================================="
echo "Output directory: $OUTPUT_DIR"
echo "Correlation ID: $CORRELATION_ID"
echo ""

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Function to safely copy file/directory
safe_copy() {
  local source=$1
  local dest=$2
  if [ -e "$source" ]; then
    cp -r "$source" "$dest"
    echo "✓ Collected: $source"
  else
    echo "⚠ Not found: $source"
  fi
}

# Function to run command and save output
run_and_save() {
  local name=$1
  local command=$2
  echo "Running: $name"
  eval "$command" >"$OUTPUT_DIR/$name.txt" 2>&1 || echo "Command failed: $command" >"$OUTPUT_DIR/$name.txt"
  echo "✓ Saved: $name.txt"
}

# 1. Collect logs
echo ""
echo "Collecting logs..."
mkdir -p "$OUTPUT_DIR/logs"

# Backend logs
safe_copy "$PROJECT_ROOT/Aura.Api/logs" "$OUTPUT_DIR/logs/api"

# Test results
safe_copy "$PROJECT_ROOT/TestResults" "$OUTPUT_DIR/test-results"
safe_copy "$PROJECT_ROOT/Aura.Web/test-results" "$OUTPUT_DIR/playwright-results"
safe_copy "$PROJECT_ROOT/Aura.Web/playwright-report" "$OUTPUT_DIR/playwright-report"

# 2. System information
echo ""
echo "Collecting system information..."
mkdir -p "$OUTPUT_DIR/system"

run_and_save "system/node-version" "node --version"
run_and_save "system/npm-version" "npm --version"
run_and_save "system/dotnet-version" "dotnet --version"
run_and_save "system/dotnet-info" "dotnet --info"
run_and_save "system/git-status" "cd $PROJECT_ROOT && git status"
run_and_save "system/git-log" "cd $PROJECT_ROOT && git log -10 --oneline"
run_and_save "system/disk-usage" "df -h"
run_and_save "system/memory-usage" "free -h 2>/dev/null || echo 'free command not available'"

# 3. Environment information
echo ""
echo "Collecting environment information..."
mkdir -p "$OUTPUT_DIR/environment"

run_and_save "environment/env-vars" "printenv | grep -E '(NODE|NPM|DOTNET|CI|GITHUB)' | sort"
run_and_save "environment/package-versions" "cd $PROJECT_ROOT/Aura.Web && npm list --depth=0"
run_and_save "environment/nuget-packages" "dotnet list $PROJECT_ROOT/Aura.sln package"

# 4. Build artifacts
echo ""
echo "Collecting build artifacts..."
mkdir -p "$OUTPUT_DIR/build"

safe_copy "$PROJECT_ROOT/Aura.Web/dist" "$OUTPUT_DIR/build/frontend-dist"
run_and_save "build/frontend-bundle-size" "du -sh $PROJECT_ROOT/Aura.Web/dist/* 2>/dev/null || echo 'No dist directory'"

# 5. Test screenshots and traces
echo ""
echo "Collecting test artifacts..."
mkdir -p "$OUTPUT_DIR/test-artifacts"

# Playwright screenshots
if [ -d "$PROJECT_ROOT/Aura.Web/test-results" ]; then
  find "$PROJECT_ROOT/Aura.Web/test-results" -name "*.png" -exec cp {} "$OUTPUT_DIR/test-artifacts/" \; 2>/dev/null || true
  find "$PROJECT_ROOT/Aura.Web/test-results" -name "*.webm" -exec cp {} "$OUTPUT_DIR/test-artifacts/" \; 2>/dev/null || true
  echo "✓ Collected Playwright screenshots and videos"
fi

# 6. API diagnostics (if API is running)
echo ""
echo "Collecting API diagnostics..."
mkdir -p "$OUTPUT_DIR/api-diagnostics"

# Try to get diagnostics from running API
API_URL="${API_URL:-http://localhost:5005}"
if curl -s --max-time 5 "$API_URL/health/live" >/dev/null 2>&1; then
  echo "API is running, collecting diagnostics..."

  curl -s "$API_URL/api/health/system" >"$OUTPUT_DIR/api-diagnostics/health-system.json" 2>&1 || true
  curl -s "$API_URL/api/health/providers" >"$OUTPUT_DIR/api-diagnostics/health-providers.json" 2>&1 || true
  curl -s "$API_URL/api/diagnostics/report" >"$OUTPUT_DIR/api-diagnostics/diagnostics-report.json" 2>&1 || true
  curl -I -s "$API_URL/api/health/system" >"$OUTPUT_DIR/api-diagnostics/health-headers.txt" 2>&1 || true

  echo "✓ Collected API diagnostics"
else
  echo "⚠ API is not running, skipping API diagnostics"
  echo "API was not reachable at $API_URL" >"$OUTPUT_DIR/api-diagnostics/not-running.txt"
fi

# 7. Contract schemas
echo ""
echo "Collecting contract schemas..."
mkdir -p "$OUTPUT_DIR/contracts"

safe_copy "$PROJECT_ROOT/tests/contracts/schemas" "$OUTPUT_DIR/contracts/schemas"

# 8. CI-specific information
if [ -n "$GITHUB_ACTIONS" ]; then
  echo ""
  echo "Collecting GitHub Actions information..."
  mkdir -p "$OUTPUT_DIR/ci"

  cat >"$OUTPUT_DIR/ci/github-context.txt" <<EOF
Workflow: $GITHUB_WORKFLOW
Run ID: $GITHUB_RUN_ID
Run Number: $GITHUB_RUN_NUMBER
Job: $GITHUB_JOB
Action: $GITHUB_ACTION
Actor: $GITHUB_ACTOR
Repository: $GITHUB_REPOSITORY
Event: $GITHUB_EVENT_NAME
Ref: $GITHUB_REF
SHA: $GITHUB_SHA
EOF
  echo "✓ Collected GitHub Actions context"
fi

# 9. Create summary
echo ""
echo "Creating diagnostic summary..."

cat >"$OUTPUT_DIR/DIAGNOSTIC_SUMMARY.txt" <<EOF
Diagnostic Bundle Summary
========================

Collection Time: $(date -u +"%Y-%m-%d %H:%M:%S UTC")
Correlation ID: $CORRELATION_ID
System: $(uname -s) $(uname -m)

Contents:
---------
- logs/             : Application and test logs
- test-results/     : Test execution results
- playwright-report/: Playwright HTML report
- test-artifacts/   : Screenshots, videos, traces
- system/           : System information
- environment/      : Environment and package versions
- build/            : Build artifacts and bundle info
- api-diagnostics/  : API health and diagnostics snapshots
- contracts/        : OpenAPI schema files
- ci/               : CI-specific information

Usage:
------
1. Review test-results/ for test failures
2. Check playwright-report/index.html for E2E test details
3. View test-artifacts/ for failure screenshots
4. Check logs/ for application errors
5. Review api-diagnostics/ for API health at time of failure

Correlation ID for Log Search:
-------------------------------
Use correlation ID "$CORRELATION_ID" to search logs for related entries.

GitHub Actions:
---------------
Run ID: $GITHUB_RUN_ID
Job: $GITHUB_JOB
Workflow: $GITHUB_WORKFLOW

EOF

echo "✓ Created diagnostic summary"

# 10. Create tarball
echo ""
echo "Creating diagnostic bundle archive..."

BUNDLE_NAME="diagnostics-$CORRELATION_ID.tar.gz"
cd "$OUTPUT_DIR/.."
tar -czf "$BUNDLE_NAME" "$(basename $OUTPUT_DIR)"

echo ""
echo "========================================="
echo "Diagnostic collection complete!"
echo "========================================="
echo "Bundle location: $(pwd)/$BUNDLE_NAME"
echo "Size: $(du -h "$BUNDLE_NAME" | cut -f1)"
echo ""
echo "To extract: tar -xzf $BUNDLE_NAME"
echo ""

# Output location for CI to upload
echo "DIAGNOSTICS_BUNDLE=$BUNDLE_NAME" >>"${GITHUB_OUTPUT:-/dev/null}"
echo "DIAGNOSTICS_PATH=$(pwd)/$BUNDLE_NAME" >>"${GITHUB_OUTPUT:-/dev/null}"
