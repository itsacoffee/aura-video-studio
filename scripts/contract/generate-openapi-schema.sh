#!/bin/bash
# Generate OpenAPI schema for contract testing

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
API_DIR="$PROJECT_ROOT/Aura.Api"
OUTPUT_DIR="$PROJECT_ROOT/tests/contracts/schemas"

echo "========================================="
echo "OpenAPI Schema Generation"
echo "========================================="

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Build the API project to ensure it's up to date
echo ""
echo "Building Aura.Api project..."
cd "$API_DIR"
dotnet build --configuration Release --no-restore

# Start the API server temporarily
echo ""
echo "Starting API server to generate schema..."
dotnet run --no-build --configuration Release --urls "http://localhost:5555" &
API_PID=$!

# Wait for API to start
echo "Waiting for API to be ready..."
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if curl -s http://localhost:5555/health/live > /dev/null 2>&1; then
        echo "API is ready!"
        break
    fi
    attempt=$((attempt + 1))
    echo "  Attempt $attempt/$max_attempts..."
    sleep 2
done

if [ $attempt -eq $max_attempts ]; then
    echo "ERROR: API failed to start within timeout"
    kill $API_PID 2>/dev/null || true
    exit 1
fi

# Download the OpenAPI schema
echo ""
echo "Downloading OpenAPI schema..."
curl -s http://localhost:5555/swagger/v1/swagger.json > "$OUTPUT_DIR/openapi-v1.json"

if [ $? -eq 0 ]; then
    echo "✓ Schema saved to: $OUTPUT_DIR/openapi-v1.json"
    
    # Pretty print and validate JSON
    if command -v jq > /dev/null 2>&1; then
        jq '.' "$OUTPUT_DIR/openapi-v1.json" > "$OUTPUT_DIR/openapi-v1.pretty.json"
        mv "$OUTPUT_DIR/openapi-v1.pretty.json" "$OUTPUT_DIR/openapi-v1.json"
        echo "✓ Schema formatted with jq"
    fi
else
    echo "ERROR: Failed to download schema"
    kill $API_PID 2>/dev/null || true
    exit 1
fi

# Stop the API server
echo ""
echo "Stopping API server..."
kill $API_PID 2>/dev/null || true
wait $API_PID 2>/dev/null || true

# Generate schema summary
echo ""
echo "========================================="
echo "Schema Summary"
echo "========================================="
if command -v jq > /dev/null 2>&1; then
    PATHS_COUNT=$(jq '.paths | length' "$OUTPUT_DIR/openapi-v1.json")
    COMPONENTS_COUNT=$(jq '.components.schemas | length' "$OUTPUT_DIR/openapi-v1.json" 2>/dev/null || echo "0")
    echo "Total endpoints: $PATHS_COUNT"
    echo "Total schemas: $COMPONENTS_COUNT"
    echo ""
    echo "Endpoints by controller:"
    jq -r '.paths | keys[]' "$OUTPUT_DIR/openapi-v1.json" | sed 's|/api/||' | cut -d'/' -f1 | sort | uniq -c | sort -rn
fi

echo ""
echo "✓ OpenAPI schema generation complete"
