#!/bin/bash
# Verify API contracts haven't changed unexpectedly

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCHEMAS_DIR="$SCRIPT_DIR/schemas"
BASELINE_SCHEMA="$SCHEMAS_DIR/openapi-v1-baseline.json"
CURRENT_SCHEMA="$SCHEMAS_DIR/openapi-v1.json"

echo "========================================="
echo "API Contract Verification"
echo "========================================="

# Check if baseline exists
if [ ! -f "$BASELINE_SCHEMA" ]; then
    echo "WARNING: No baseline schema found"
    echo "Creating baseline from current schema..."
    if [ -f "$CURRENT_SCHEMA" ]; then
        cp "$CURRENT_SCHEMA" "$BASELINE_SCHEMA"
        echo "✓ Baseline created at: $BASELINE_SCHEMA"
        echo ""
        echo "This is the first run. Future runs will compare against this baseline."
        exit 0
    else
        echo "ERROR: No current schema found. Run generate-openapi-schema.sh first."
        exit 1
    fi
fi

# Generate current schema
echo ""
echo "Generating current API schema..."
cd "$SCRIPT_DIR/../../scripts/contract"
bash generate-openapi-schema.sh

# Compare schemas
echo ""
echo "Comparing schemas..."
if command -v jq > /dev/null 2>&1; then
    # Extract just the paths and components for comparison
    BASELINE_PATHS=$(jq -S '.paths' "$BASELINE_SCHEMA")
    CURRENT_PATHS=$(jq -S '.paths' "$CURRENT_SCHEMA")
    
    BASELINE_SCHEMAS=$(jq -S '.components.schemas // {}' "$BASELINE_SCHEMA")
    CURRENT_SCHEMAS=$(jq -S '.components.schemas // {}' "$CURRENT_SCHEMA")
    
    # Check for differences
    if [ "$BASELINE_PATHS" != "$CURRENT_PATHS" ]; then
        echo ""
        echo "⚠ CONTRACT CHANGE DETECTED: API endpoints have changed"
        echo ""
        echo "Baseline endpoints:"
        echo "$BASELINE_PATHS" | jq -r 'keys[]' | sort
        echo ""
        echo "Current endpoints:"
        echo "$CURRENT_PATHS" | jq -r 'keys[]' | sort
        echo ""
        
        # Show detailed diff
        if command -v diff > /dev/null 2>&1; then
            echo "Detailed endpoint diff:"
            diff <(echo "$BASELINE_PATHS" | jq -S '.') <(echo "$CURRENT_PATHS" | jq -S '.') || true
        fi
        
        echo ""
        echo "If this change is intentional:"
        echo "  1. Review the changes carefully"
        echo "  2. Update baseline: cp $CURRENT_SCHEMA $BASELINE_SCHEMA"
        echo "  3. Document the breaking changes in CHANGELOG.md"
        exit 1
    fi
    
    if [ "$BASELINE_SCHEMAS" != "$CURRENT_SCHEMAS" ]; then
        echo ""
        echo "⚠ CONTRACT CHANGE DETECTED: API schemas have changed"
        echo ""
        
        # Show detailed diff
        if command -v diff > /dev/null 2>&1; then
            echo "Detailed schema diff:"
            diff <(echo "$BASELINE_SCHEMAS" | jq -S '.') <(echo "$CURRENT_SCHEMAS" | jq -S '.') || true
        fi
        
        echo ""
        echo "If this change is intentional:"
        echo "  1. Review the changes carefully"
        echo "  2. Update baseline: cp $CURRENT_SCHEMA $BASELINE_SCHEMA"
        echo "  3. Document the breaking changes in CHANGELOG.md"
        exit 1
    fi
    
    echo "✓ No contract changes detected"
    echo "✓ API is backward compatible"
else
    echo "WARNING: jq not found, performing basic comparison"
    if ! diff -q "$BASELINE_SCHEMA" "$CURRENT_SCHEMA" > /dev/null 2>&1; then
        echo ""
        echo "⚠ SCHEMA FILES DIFFER"
        echo "Install jq for detailed comparison or review changes manually."
        exit 1
    fi
    echo "✓ Schemas match (basic comparison)"
fi

echo ""
echo "========================================="
echo "Contract verification PASSED"
echo "========================================="
