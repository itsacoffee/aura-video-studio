#!/bin/bash
# scripts/check_coverage_thresholds.sh
# Checks code coverage thresholds for .NET projects

set -e

THRESHOLD=60
COVERAGE_FILES=$(find . -name "coverage.cobertura.xml" 2>/dev/null || true)

if [ -z "$COVERAGE_FILES" ]; then
  echo "⚠️  No coverage files found. Skipping coverage check."
  exit 0
fi

echo "Checking .NET code coverage thresholds..."
echo "Minimum required: ${THRESHOLD}%"
echo ""

FAILED=0

for FILE in $COVERAGE_FILES; do
  echo "Checking: $FILE"

  # Extract line-rate from XML (this is a decimal, e.g., 0.75 for 75%)
  LINE_RATE=$(grep -oP 'line-rate="\K[0-9.]+' "$FILE" | head -1)

  if [ -z "$LINE_RATE" ]; then
    echo "  ⚠️  Could not extract coverage from file"
    continue
  fi

  # Convert to percentage
  COVERAGE=$(echo "$LINE_RATE * 100" | bc -l | xargs printf "%.1f")

  echo "  Coverage: ${COVERAGE}%"

  # Compare using bc for floating point comparison
  if (($(echo "$COVERAGE < $THRESHOLD" | bc -l))); then
    echo "  ❌ FAIL: Coverage ${COVERAGE}% is below threshold ${THRESHOLD}%"
    FAILED=1
  else
    echo "  ✅ PASS: Coverage meets threshold"
  fi

  echo ""
done

if [ $FAILED -eq 1 ]; then
  echo "❌ Coverage check failed. Some files are below the ${THRESHOLD}% threshold."
  exit 1
else
  echo "✅ All coverage checks passed!"
  exit 0
fi
