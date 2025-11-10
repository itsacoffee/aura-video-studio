#!/bin/bash
# Deployment Validation Script
# Comprehensive validation suite for post-deployment checks

set -euo pipefail

ENVIRONMENT="${1:-staging}"
BASE_URL="${2:-https://${ENVIRONMENT}.aura.studio}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Test results
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

echo -e "${BLUE}=========================================${NC}"
echo -e "${BLUE}Deployment Validation${NC}"
echo -e "${BLUE}=========================================${NC}"
echo "Environment: ${ENVIRONMENT}"
echo "Base URL: ${BASE_URL}"
echo ""

# Function to run a test
run_test() {
    local test_name=$1
    local test_command=$2
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    echo -n "Testing ${test_name}... "
    
    if eval "$test_command" > /dev/null 2>&1; then
        echo -e "${GREEN}PASS${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
        return 0
    else
        echo -e "${RED}FAIL${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
        return 1
    fi
}

# Health Checks
echo -e "${BLUE}Health Checks${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

run_test "API liveness" "curl -sf ${BASE_URL}/api/health/live"
run_test "API readiness" "curl -sf ${BASE_URL}/api/health/ready"
run_test "API system health" "curl -sf ${BASE_URL}/api/health/system"
run_test "Web frontend" "curl -sf ${BASE_URL}/"
run_test "Web health endpoint" "curl -sf ${BASE_URL}/health"

echo ""

# API Endpoint Tests
echo -e "${BLUE}API Endpoint Tests${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

run_test "Version endpoint" "curl -sf ${BASE_URL}/api/version"
run_test "Diagnostics endpoint" "curl -sf ${BASE_URL}/api/diagnostics/system"
run_test "Health providers" "curl -sf ${BASE_URL}/api/health/providers"
run_test "Feature flags" "curl -sf ${BASE_URL}/api/featureflags"

echo ""

# Performance Tests
echo -e "${BLUE}Performance Tests${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Response time test
test_response_time() {
    local endpoint=$1
    local threshold_ms=$2
    
    local start_time=$(date +%s%N)
    curl -sf "$endpoint" > /dev/null 2>&1
    local end_time=$(date +%s%N)
    
    local response_time=$(( (end_time - start_time) / 1000000 ))
    
    if [ $response_time -lt $threshold_ms ]; then
        echo "Response time: ${response_time}ms (< ${threshold_ms}ms)"
        return 0
    else
        echo "Response time: ${response_time}ms (>= ${threshold_ms}ms)"
        return 1
    fi
}

echo -n "API response time (< 2000ms)... "
if test_response_time "${BASE_URL}/api/health/live" 2000; then
    echo -e "${GREEN}PASS${NC}"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    echo -e "${YELLOW}WARN${NC}"
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

echo -n "Web response time (< 3000ms)... "
if test_response_time "${BASE_URL}/" 3000; then
    echo -e "${GREEN}PASS${NC}"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    echo -e "${YELLOW}WARN${NC}"
fi
TOTAL_TESTS=$((TOTAL_TESTS + 1))

echo ""

# Security Tests
echo -e "${BLUE}Security Tests${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

test_security_header() {
    local header=$1
    local response=$(curl -sI "${BASE_URL}" 2>/dev/null)
    
    if echo "$response" | grep -qi "$header"; then
        return 0
    else
        return 1
    fi
}

run_test "HTTPS redirect" "curl -sf -I ${BASE_URL} | grep -qi 'strict-transport-security' || [ '${ENVIRONMENT}' == 'local' ]"
run_test "X-Content-Type-Options header" "test_security_header 'X-Content-Type-Options'"
run_test "X-Frame-Options header" "test_security_header 'X-Frame-Options'"

echo ""

# Functional Tests
echo -e "${BLUE}Functional Tests${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Test version matches deployment
test_version() {
    local expected_version=$1
    
    if [ -z "$expected_version" ]; then
        return 0
    fi
    
    local actual_version=$(curl -s "${BASE_URL}/api/version" | jq -r '.version' 2>/dev/null || echo "")
    
    if [ "$actual_version" == "$expected_version" ]; then
        echo "Version matches: ${actual_version}"
        return 0
    else
        echo "Version mismatch: expected ${expected_version}, got ${actual_version}"
        return 1
    fi
}

echo -n "Version check... "
if [ -n "${DEPLOY_VERSION:-}" ]; then
    if test_version "$DEPLOY_VERSION"; then
        echo -e "${GREEN}PASS${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "${RED}FAIL${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
else
    echo -e "${YELLOW}SKIP (no version specified)${NC}"
fi

# Test database connectivity
run_test "Database connectivity" "curl -sf ${BASE_URL}/api/diagnostics/system | jq -e '.database.connected' > /dev/null"

# Test Redis connectivity
run_test "Redis connectivity" "curl -sf ${BASE_URL}/api/diagnostics/system | jq -e '.cache.connected' > /dev/null"

echo ""

# Load Test (basic)
echo -e "${BLUE}Load Test (basic)${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

echo -n "Concurrent requests test (10 requests)... "
success_count=0
for i in {1..10}; do
    if curl -sf "${BASE_URL}/api/health/live" > /dev/null 2>&1; then
        success_count=$((success_count + 1))
    fi &
done
wait

TOTAL_TESTS=$((TOTAL_TESTS + 1))
if [ $success_count -eq 10 ]; then
    echo -e "${GREEN}PASS (10/10)${NC}"
    PASSED_TESTS=$((PASSED_TESTS + 1))
else
    echo -e "${RED}FAIL (${success_count}/10)${NC}"
    FAILED_TESTS=$((FAILED_TESTS + 1))
fi

echo ""

# Integration Tests
echo -e "${BLUE}Integration Tests${NC}"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Test CORS
test_cors() {
    local response=$(curl -sI -H "Origin: https://example.com" "${BASE_URL}/api/health/live" 2>/dev/null)
    
    if echo "$response" | grep -qi "access-control-allow"; then
        return 0
    else
        # CORS might be disabled in production
        [ "${ENVIRONMENT}" == "production" ] && return 0
        return 1
    fi
}

run_test "CORS configuration" "test_cors"

# Test API documentation
run_test "Swagger/OpenAPI docs" "curl -sf ${BASE_URL}/swagger/index.html > /dev/null || [ '${ENVIRONMENT}' == 'production' ]"

echo ""

# Summary
echo -e "${BLUE}=========================================${NC}"
echo -e "${BLUE}Validation Summary${NC}"
echo -e "${BLUE}=========================================${NC}"
echo "Total tests: ${TOTAL_TESTS}"
echo -e "${GREEN}Passed: ${PASSED_TESTS}${NC}"
echo -e "${RED}Failed: ${FAILED_TESTS}${NC}"

success_rate=$((PASSED_TESTS * 100 / TOTAL_TESTS))
echo "Success rate: ${success_rate}%"
echo ""

# Exit with error if any tests failed
if [ $FAILED_TESTS -gt 0 ]; then
    echo -e "${RED}❌ Validation FAILED${NC}"
    echo "Some tests failed. Please review the deployment."
    exit 1
else
    echo -e "${GREEN}✅ Validation PASSED${NC}"
    echo "All tests passed successfully!"
    exit 0
fi
