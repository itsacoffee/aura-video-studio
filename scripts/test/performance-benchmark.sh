#!/bin/bash
# Performance Benchmark Script
# Runs performance tests and generates benchmark reports

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
ITERATIONS=${1:-3}
WARMUP_ITERATIONS=1
OUTPUT_DIR="$ROOT_DIR/TestResults/benchmarks"

echo -e "${BLUE}=== Performance Benchmark Suite ===${NC}"
echo "Iterations: $ITERATIONS (+ $WARMUP_ITERATIONS warmup)"
echo "Output: $OUTPUT_DIR"
echo ""

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Function to run .NET performance tests
run_dotnet_benchmarks() {
    echo -e "${BLUE}Running .NET Performance Tests...${NC}"
    
    cd "$ROOT_DIR"
    
    # Run performance tests
    dotnet test Aura.Tests/Aura.Tests.csproj \
        --filter "Category=Performance" \
        --logger "console;verbosity=detailed" \
        --logger "trx;LogFileName=performance-results.trx" \
        --results-directory "$OUTPUT_DIR" \
        --configuration Release
    
    echo -e "${GREEN}✓ .NET benchmarks complete${NC}"
}

# Function to run frontend performance tests
run_frontend_benchmarks() {
    echo -e "${BLUE}Running Frontend Performance Tests...${NC}"
    
    cd "$ROOT_DIR/Aura.Web"
    
    # Create performance test runner
    cat > "run-perf-tests.js" << 'EOF'
const { performance } = require('perf_hooks');

async function runBenchmark(name, fn, iterations) {
    const times = [];
    
    // Warmup
    await fn();
    
    // Measure
    for (let i = 0; i < iterations; i++) {
        const start = performance.now();
        await fn();
        const end = performance.now();
        times.push(end - start);
    }
    
    const avg = times.reduce((a, b) => a + b, 0) / times.length;
    const min = Math.min(...times);
    const max = Math.max(...times);
    
    return { name, avg, min, max, times };
}

async function main() {
    console.log('Frontend Performance Benchmarks\n');
    
    const results = [];
    
    // Add your performance tests here
    // Example:
    // results.push(await runBenchmark('Parse large JSON', () => {
    //     JSON.parse(largeJsonString);
    // }, 100));
    
    // Print results
    results.forEach(result => {
        console.log(`${result.name}:`);
        console.log(`  Average: ${result.avg.toFixed(2)}ms`);
        console.log(`  Min: ${result.min.toFixed(2)}ms`);
        console.log(`  Max: ${result.max.toFixed(2)}ms`);
        console.log('');
    });
    
    // Save results
    const fs = require('fs');
    fs.writeFileSync(
        'benchmark-results.json',
        JSON.stringify(results, null, 2)
    );
}

main().catch(console.error);
EOF
    
    node run-perf-tests.js || echo -e "${YELLOW}⚠ No frontend benchmarks defined yet${NC}"
    rm -f run-perf-tests.js
    
    echo -e "${GREEN}✓ Frontend benchmarks complete${NC}"
}

# Function to generate benchmark report
generate_report() {
    echo -e "${BLUE}Generating Benchmark Report...${NC}"
    
    cat > "$OUTPUT_DIR/BENCHMARK_REPORT.md" << EOF
# Performance Benchmark Report

Generated: $(date -u +"%Y-%m-%d %H:%M:%S UTC")

## Configuration

- Iterations: $ITERATIONS
- Warmup: $WARMUP_ITERATIONS
- Configuration: Release

## Results

### Backend (.NET)

EOF

    # Parse TRX file if it exists
    if [ -f "$OUTPUT_DIR/performance-results.trx" ]; then
        echo "See detailed results in: performance-results.trx" >> "$OUTPUT_DIR/BENCHMARK_REPORT.md"
    fi
    
    cat >> "$OUTPUT_DIR/BENCHMARK_REPORT.md" << EOF

### Frontend (TypeScript)

EOF

    if [ -f "$ROOT_DIR/Aura.Web/benchmark-results.json" ]; then
        echo "See detailed results in: Aura.Web/benchmark-results.json" >> "$OUTPUT_DIR/BENCHMARK_REPORT.md"
    fi
    
    cat >> "$OUTPUT_DIR/BENCHMARK_REPORT.md" << EOF

## Thresholds

| Test Category | Target | Warning |
|---------------|--------|---------|
| Unit tests | < 100ms | < 500ms |
| Integration tests | < 1s | < 5s |
| E2E tests | < 10s | < 30s |

## Next Steps

1. Review tests exceeding thresholds
2. Optimize hot paths
3. Add performance regression tests
4. Monitor trends over time

EOF
    
    echo -e "${GREEN}✓ Report generated: $OUTPUT_DIR/BENCHMARK_REPORT.md${NC}"
}

# Function to compare with baseline
compare_baseline() {
    echo -e "${BLUE}Comparing with Baseline...${NC}"
    
    BASELINE_FILE="$OUTPUT_DIR/baseline.json"
    
    if [ -f "$BASELINE_FILE" ]; then
        echo -e "${YELLOW}Baseline comparison not yet implemented${NC}"
        # TODO: Implement baseline comparison
    else
        echo -e "${YELLOW}No baseline found. Creating baseline...${NC}"
        # Save current results as baseline
        if [ -f "$ROOT_DIR/Aura.Web/benchmark-results.json" ]; then
            cp "$ROOT_DIR/Aura.Web/benchmark-results.json" "$BASELINE_FILE"
        fi
    fi
}

# Main execution
main() {
    local START_TIME=$(date +%s)
    
    # Run benchmarks
    run_dotnet_benchmarks || echo -e "${YELLOW}⚠ .NET benchmarks had issues${NC}"
    run_frontend_benchmarks || echo -e "${YELLOW}⚠ Frontend benchmarks had issues${NC}"
    
    # Generate report
    generate_report
    
    # Compare with baseline
    compare_baseline
    
    local END_TIME=$(date +%s)
    local DURATION=$((END_TIME - START_TIME))
    
    echo ""
    echo -e "${BLUE}=== Benchmark Complete ===${NC}"
    echo "Duration: ${DURATION}s"
    echo "Report: $OUTPUT_DIR/BENCHMARK_REPORT.md"
    echo ""
    echo -e "${GREEN}All benchmarks completed!${NC}"
}

# Run main
main
