#!/bin/bash
# Test Coverage Analysis Script
# Analyzes test coverage and generates detailed reports

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Coverage thresholds
COVERAGE_THRESHOLD=80
WARNING_THRESHOLD=70

echo -e "${BLUE}=== Test Coverage Analysis ===${NC}"
echo ""

# Function to analyze .NET coverage
analyze_dotnet_coverage() {
    echo -e "${BLUE}Analyzing .NET Coverage...${NC}"
    
    cd "$ROOT_DIR"
    
    # Run tests with coverage
    dotnet test Aura.Tests/Aura.Tests.csproj \
        --collect:"XPlat Code Coverage" \
        --results-directory:"$ROOT_DIR/TestResults" \
        --configuration Release \
        --no-build \
        --verbosity minimal \
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura,opencover,json
    
    # Find coverage files
    COVERAGE_FILE=$(find "$ROOT_DIR/TestResults" -name "coverage.cobertura.xml" | head -1)
    
    if [ -z "$COVERAGE_FILE" ]; then
        echo -e "${RED}No coverage file found!${NC}"
        return 1
    fi
    
    # Generate HTML report
    if command -v reportgenerator &> /dev/null; then
        reportgenerator \
            -reports:"$ROOT_DIR/TestResults/**/coverage.cobertura.xml" \
            -targetdir:"$ROOT_DIR/TestResults/coverage-report" \
            -reporttypes:"Html;JsonSummary;Badges;TextSummary;MarkdownSummary" \
            -assemblyfilters:"+Aura.*;-*.Tests" \
            -classfilters:"-*Tests;-*TestBase;-Program;-*.Migrations.*"
        
        echo -e "${GREEN}✓ HTML report generated: TestResults/coverage-report/index.html${NC}"
    fi
    
    # Parse coverage percentage from JSON summary
    SUMMARY_FILE="$ROOT_DIR/TestResults/coverage-report/Summary.json"
    if [ -f "$SUMMARY_FILE" ]; then
        LINE_COVERAGE=$(cat "$SUMMARY_FILE" | grep -oP '"linecoverage":\s*\K[0-9.]+' | head -1)
        BRANCH_COVERAGE=$(cat "$SUMMARY_FILE" | grep -oP '"branchcoverage":\s*\K[0-9.]+' | head -1)
        
        echo ""
        echo -e "${BLUE}.NET Coverage Results:${NC}"
        echo "  Line Coverage:   ${LINE_COVERAGE}%"
        echo "  Branch Coverage: ${BRANCH_COVERAGE}%"
        echo ""
        
        # Check thresholds
        if (( $(echo "$LINE_COVERAGE >= $COVERAGE_THRESHOLD" | bc -l) )); then
            echo -e "${GREEN}✓ .NET coverage meets threshold (>=${COVERAGE_THRESHOLD}%)${NC}"
        elif (( $(echo "$LINE_COVERAGE >= $WARNING_THRESHOLD" | bc -l) )); then
            echo -e "${YELLOW}⚠ .NET coverage below threshold but above warning level${NC}"
        else
            echo -e "${RED}✗ .NET coverage below warning threshold${NC}"
            return 1
        fi
    fi
}

# Function to analyze Frontend coverage
analyze_frontend_coverage() {
    echo -e "${BLUE}Analyzing Frontend Coverage...${NC}"
    
    cd "$ROOT_DIR/Aura.Web"
    
    # Run tests with coverage
    npm run test:coverage -- --reporter=json --reporter=text
    
    # Check if coverage directory exists
    if [ ! -d "coverage" ]; then
        echo -e "${RED}No coverage directory found!${NC}"
        return 1
    fi
    
    # Parse coverage from JSON
    SUMMARY_FILE="coverage/coverage-summary.json"
    if [ -f "$SUMMARY_FILE" ]; then
        TOTAL_LINES=$(cat "$SUMMARY_FILE" | grep -oP '"lines":\s*\{\s*"total":\s*\K[0-9]+' | head -1)
        COVERED_LINES=$(cat "$SUMMARY_FILE" | grep -oP '"lines":\s*\{\s*"total":\s*[0-9]+,\s*"covered":\s*\K[0-9]+' | head -1)
        LINE_COVERAGE=$(cat "$SUMMARY_FILE" | grep -oP '"lines":\s*\{[^}]*"pct":\s*\K[0-9.]+' | head -1)
        BRANCH_COVERAGE=$(cat "$SUMMARY_FILE" | grep -oP '"branches":\s*\{[^}]*"pct":\s*\K[0-9.]+' | head -1)
        FUNCTION_COVERAGE=$(cat "$SUMMARY_FILE" | grep -oP '"functions":\s*\{[^}]*"pct":\s*\K[0-9.]+' | head -1)
        
        echo ""
        echo -e "${BLUE}Frontend Coverage Results:${NC}"
        echo "  Line Coverage:     ${LINE_COVERAGE}%"
        echo "  Branch Coverage:   ${BRANCH_COVERAGE}%"
        echo "  Function Coverage: ${FUNCTION_COVERAGE}%"
        echo "  Covered Lines:     ${COVERED_LINES}/${TOTAL_LINES}"
        echo ""
        
        # Check thresholds
        if (( $(echo "$LINE_COVERAGE >= $COVERAGE_THRESHOLD" | bc -l) )); then
            echo -e "${GREEN}✓ Frontend coverage meets threshold (>=${COVERAGE_THRESHOLD}%)${NC}"
        elif (( $(echo "$LINE_COVERAGE >= $WARNING_THRESHOLD" | bc -l) )); then
            echo -e "${YELLOW}⚠ Frontend coverage below threshold but above warning level${NC}"
        else
            echo -e "${RED}✗ Frontend coverage below warning threshold${NC}"
            return 1
        fi
    fi
    
    echo -e "${GREEN}✓ HTML report available: coverage/index.html${NC}"
}

# Function to identify uncovered areas
identify_gaps() {
    echo ""
    echo -e "${BLUE}=== Identifying Coverage Gaps ===${NC}"
    
    # .NET gaps
    if [ -f "$ROOT_DIR/TestResults/coverage-report/Summary.txt" ]; then
        echo ""
        echo -e "${YELLOW}Top 10 .NET Files with Lowest Coverage:${NC}"
        grep -A 100 "Classes" "$ROOT_DIR/TestResults/coverage-report/Summary.txt" | \
            grep -E "^\s+[0-9]" | \
            sort -k2 -n | \
            head -10
    fi
    
    # Frontend gaps
    if [ -f "$ROOT_DIR/Aura.Web/coverage/coverage-summary.json" ]; then
        echo ""
        echo -e "${YELLOW}Files with Coverage < 60%:${NC}"
        cd "$ROOT_DIR/Aura.Web"
        node -e "
            const fs = require('fs');
            const coverage = JSON.parse(fs.readFileSync('coverage/coverage-summary.json', 'utf8'));
            
            const lowCoverage = Object.entries(coverage)
                .filter(([file]) => file !== 'total')
                .map(([file, metrics]) => ({
                    file: file.replace('$ROOT_DIR/Aura.Web/', ''),
                    coverage: metrics.lines.pct
                }))
                .filter(item => item.coverage < 60)
                .sort((a, b) => a.coverage - b.coverage);
            
            lowCoverage.forEach(item => {
                console.log(\`  \${item.file}: \${item.coverage}%\`);
            });
        " 2>/dev/null || echo "  (Node.js required for detailed analysis)"
    fi
}

# Function to generate coverage badge
generate_badge() {
    echo ""
    echo -e "${BLUE}Generating Coverage Badges...${NC}"
    
    # Create badges directory
    mkdir -p "$ROOT_DIR/docs/badges"
    
    # Generate .NET badge
    if [ ! -z "$LINE_COVERAGE" ]; then
        local COLOR="red"
        if (( $(echo "$LINE_COVERAGE >= $COVERAGE_THRESHOLD" | bc -l) )); then
            COLOR="brightgreen"
        elif (( $(echo "$LINE_COVERAGE >= $WARNING_THRESHOLD" | bc -l) )); then
            COLOR="yellow"
        fi
        
        echo "[![Backend Coverage](https://img.shields.io/badge/backend%20coverage-${LINE_COVERAGE}%25-${COLOR})]()" \
            > "$ROOT_DIR/docs/badges/backend-coverage.md"
    fi
    
    echo -e "${GREEN}✓ Badges generated${NC}"
}

# Function to generate summary report
generate_summary() {
    echo ""
    echo -e "${BLUE}=== Coverage Summary ===${NC}"
    
    cat > "$ROOT_DIR/COVERAGE_REPORT.md" << EOF
# Test Coverage Report

Generated: $(date -u +"%Y-%m-%d %H:%M:%S UTC")

## Overall Coverage

| Component | Line Coverage | Branch Coverage | Status |
|-----------|---------------|-----------------|--------|
| Backend (.NET) | ${LINE_COVERAGE:-N/A}% | ${BRANCH_COVERAGE:-N/A}% | $([ ! -z "$LINE_COVERAGE" ] && (( $(echo "$LINE_COVERAGE >= $COVERAGE_THRESHOLD" | bc -l) )) && echo "✅" || echo "⚠️") |
| Frontend (React) | ${LINE_COVERAGE:-N/A}% | ${BRANCH_COVERAGE:-N/A}% | $([ ! -z "$LINE_COVERAGE" ] && (( $(echo "$LINE_COVERAGE >= $COVERAGE_THRESHOLD" | bc -l) )) && echo "✅" || echo "⚠️") |

## Thresholds

- **Target**: ${COVERAGE_THRESHOLD}%
- **Warning**: ${WARNING_THRESHOLD}%

## Reports

- [Backend HTML Report](./TestResults/coverage-report/index.html)
- [Frontend HTML Report](./Aura.Web/coverage/index.html)

## Next Steps

EOF

    if (( $(echo "${LINE_COVERAGE:-0} < $COVERAGE_THRESHOLD" | bc -l) )); then
        cat >> "$ROOT_DIR/COVERAGE_REPORT.md" << EOF
### Priority Actions

1. Add tests for uncovered files (see gaps above)
2. Focus on critical paths first
3. Add edge case tests
4. Review and improve integration tests

EOF
    else
        cat >> "$ROOT_DIR/COVERAGE_REPORT.md" << EOF
### Maintaining Coverage

1. Continue writing tests for new features
2. Monitor coverage trends
3. Address any regressions promptly
4. Keep tests maintainable and fast

EOF
    fi
    
    echo -e "${GREEN}✓ Summary report generated: COVERAGE_REPORT.md${NC}"
}

# Main execution
main() {
    local BACKEND_RESULT=0
    local FRONTEND_RESULT=0
    
    # Analyze backend
    analyze_dotnet_coverage || BACKEND_RESULT=$?
    
    # Analyze frontend
    analyze_frontend_coverage || FRONTEND_RESULT=$?
    
    # Identify gaps
    identify_gaps
    
    # Generate badge
    generate_badge
    
    # Generate summary
    generate_summary
    
    echo ""
    echo -e "${BLUE}=== Analysis Complete ===${NC}"
    
    # Exit with error if either failed
    if [ $BACKEND_RESULT -ne 0 ] || [ $FRONTEND_RESULT -ne 0 ]; then
        echo -e "${RED}Coverage analysis found issues${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}All coverage checks passed!${NC}"
}

# Run main function
main
