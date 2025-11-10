#!/bin/bash
# Blue-Green Deployment Script
# Provides zero-downtime deployment by maintaining two identical environments

set -euo pipefail

# Configuration
ENVIRONMENT="${1:-staging}"
VERSION="${2:-latest}"
TIMEOUT="${3:-10m}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=========================================${NC}"
echo -e "${BLUE}Blue-Green Deployment${NC}"
echo -e "${BLUE}=========================================${NC}"
echo "Environment: $ENVIRONMENT"
echo "Version: $VERSION"
echo "Timeout: $TIMEOUT"
echo ""

# Parse timeout to seconds
timeout_seconds=600  # default 10 minutes
if [[ $TIMEOUT =~ ([0-9]+)m ]]; then
    timeout_seconds=$((${BASH_REMATCH[1]} * 60))
elif [[ $TIMEOUT =~ ([0-9]+)s ]]; then
    timeout_seconds=${BASH_REMATCH[1]}
fi

# Determine active/inactive environments
get_active_environment() {
    # Query load balancer or service discovery to determine active environment
    # For this example, we'll use a state file
    local state_file="${PROJECT_ROOT}/deploy/.deployment-state-${ENVIRONMENT}"
    
    if [ -f "$state_file" ]; then
        cat "$state_file"
    else
        echo "blue"
    fi
}

set_active_environment() {
    local env=$1
    local state_file="${PROJECT_ROOT}/deploy/.deployment-state-${ENVIRONMENT}"
    echo "$env" > "$state_file"
}

# Function to check health endpoint
check_health() {
    local endpoint=$1
    local max_attempts=${2:-30}
    local attempt=0
    
    echo "Checking health: $endpoint"
    
    while [ $attempt -lt $max_attempts ]; do
        attempt=$((attempt + 1))
        
        response=$(curl -s -w "\n%{http_code}" "$endpoint" 2>/dev/null || echo "000")
        body=$(echo "$response" | head -n -1)
        status=$(echo "$response" | tail -n 1)
        
        if [ "$status" = "200" ]; then
            echo -e "${GREEN}✓ Health check passed (attempt $attempt/$max_attempts)${NC}"
            return 0
        fi
        
        echo "Health check attempt $attempt/$max_attempts failed (HTTP $status)"
        sleep 10
    done
    
    echo -e "${RED}✗ Health check failed after $max_attempts attempts${NC}"
    return 1
}

# Function to deploy to environment
deploy_to_environment() {
    local target_env=$1
    local version=$2
    
    echo ""
    echo -e "${BLUE}Deploying to ${target_env} environment...${NC}"
    
    # Set environment-specific variables
    local compose_file="${PROJECT_ROOT}/docker-compose.prod.yml"
    local project_name="aura-${ENVIRONMENT}-${target_env}"
    
    # Export environment variables
    export VERSION="$version"
    export DOCKER_REGISTRY="${DOCKER_REGISTRY:-ghcr.io/aura}"
    export API_PORT=$((target_env == "blue" ? 5005 : 5006))
    export WEB_PORT=$((target_env == "blue" ? 3000 : 3001))
    
    # Pull latest images
    echo "Pulling Docker images..."
    docker compose -f "$compose_file" -p "$project_name" pull
    
    # Start services
    echo "Starting services..."
    docker compose -f "$compose_file" -p "$project_name" up -d --remove-orphans
    
    # Wait for services to be ready
    echo "Waiting for services to be ready..."
    sleep 30
    
    # Check health
    local api_url="http://localhost:${API_PORT}"
    if ! check_health "${api_url}/health/live" 30; then
        echo -e "${RED}✗ API health check failed${NC}"
        return 1
    fi
    
    echo -e "${GREEN}✓ Deployment to ${target_env} environment successful${NC}"
    return 0
}

# Function to switch traffic
switch_traffic() {
    local from_env=$1
    local to_env=$2
    
    echo ""
    echo -e "${BLUE}Switching traffic from ${from_env} to ${to_env}...${NC}"
    
    # Update load balancer configuration
    # This is environment-specific and depends on your infrastructure
    # Examples:
    # - Update nginx upstream configuration
    # - Update cloud load balancer target groups
    # - Update service mesh routing rules
    # - Update DNS records
    
    if [ "$ENVIRONMENT" == "production" ]; then
        # Production traffic switch
        echo "Updating production load balancer..."
        # kubectl set image deployment/aura-web aura-web=...
        # or
        # az webapp deployment slot swap --name aura --resource-group aura-prod --slot staging
        # or
        # AWS ELB target group update
    else
        # Staging traffic switch
        echo "Updating staging load balancer..."
        # Update nginx configuration
        cat > "${PROJECT_ROOT}/deploy/nginx/upstream.conf" <<EOF
upstream aura_backend {
    server api_${to_env}:5005;
}

upstream aura_frontend {
    server web_${to_env}:80;
}
EOF
        
        # Reload nginx
        docker exec aura-loadbalancer nginx -s reload 2>/dev/null || true
    fi
    
    echo -e "${GREEN}✓ Traffic switched to ${to_env} environment${NC}"
}

# Function to validate deployment
validate_deployment() {
    local env=$1
    local duration=${2:-300}  # 5 minutes default
    
    echo ""
    echo -e "${BLUE}Validating ${env} environment for ${duration}s...${NC}"
    
    local api_port=$((env == "blue" ? 5005 : 5006))
    local api_url="http://localhost:${api_port}"
    
    local start_time=$(date +%s)
    local check_count=0
    local failed_checks=0
    
    while [ $(($(date +%s) - start_time)) -lt $duration ]; do
        check_count=$((check_count + 1))
        
        echo -n "  Validation check $check_count: "
        
        # System health check
        if ! curl -sf "${api_url}/health/system" > /dev/null 2>&1; then
            echo -e "${RED}FAILED${NC}"
            failed_checks=$((failed_checks + 1))
            
            if [ $failed_checks -ge 3 ]; then
                echo -e "${RED}✗ Multiple validation failures detected${NC}"
                return 1
            fi
        else
            echo -e "${GREEN}PASSED${NC}"
            failed_checks=0
        fi
        
        sleep 30
    done
    
    echo -e "${GREEN}✓ Validation completed successfully${NC}"
    return 0
}

# Function to rollback
rollback() {
    local from_env=$1
    local to_env=$2
    
    echo ""
    echo -e "${RED}=========================================${NC}"
    echo -e "${RED}ROLLBACK TRIGGERED${NC}"
    echo -e "${RED}=========================================${NC}"
    
    echo "Rolling back from ${from_env} to ${to_env}..."
    
    # Switch traffic back
    switch_traffic "$from_env" "$to_env"
    
    # Update state
    set_active_environment "$to_env"
    
    # Stop failed environment
    local project_name="aura-${ENVIRONMENT}-${from_env}"
    echo "Stopping ${from_env} environment..."
    docker compose -p "$project_name" down
    
    echo -e "${GREEN}✓ Rollback complete${NC}"
    exit 1
}

# Main deployment flow
main() {
    # Get current active environment
    ACTIVE_ENV=$(get_active_environment)
    
    if [ "$ACTIVE_ENV" == "blue" ]; then
        INACTIVE_ENV="green"
    else
        INACTIVE_ENV="blue"
    fi
    
    echo "Current active environment: ${ACTIVE_ENV}"
    echo "Deploying to: ${INACTIVE_ENV}"
    echo ""
    
    # Step 1: Deploy to inactive environment
    if ! deploy_to_environment "$INACTIVE_ENV" "$VERSION"; then
        echo -e "${RED}✗ Deployment to ${INACTIVE_ENV} failed${NC}"
        exit 1
    fi
    
    # Step 2: Validate inactive environment
    if ! validate_deployment "$INACTIVE_ENV" 180; then
        echo -e "${RED}✗ Validation of ${INACTIVE_ENV} failed${NC}"
        
        # Cleanup
        local project_name="aura-${ENVIRONMENT}-${INACTIVE_ENV}"
        docker compose -p "$project_name" down
        
        exit 1
    fi
    
    # Step 3: Switch traffic to new environment
    switch_traffic "$ACTIVE_ENV" "$INACTIVE_ENV"
    
    # Step 4: Monitor new environment
    echo ""
    echo "Monitoring new active environment for issues..."
    if ! validate_deployment "$INACTIVE_ENV" 300; then
        rollback "$INACTIVE_ENV" "$ACTIVE_ENV"
    fi
    
    # Step 5: Update state
    set_active_environment "$INACTIVE_ENV"
    
    # Step 6: Keep old environment running for quick rollback (optional)
    echo ""
    echo "Keeping ${ACTIVE_ENV} environment running for 5 minutes for quick rollback..."
    sleep 300
    
    # Step 7: Cleanup old environment
    echo "Stopping old ${ACTIVE_ENV} environment..."
    local old_project_name="aura-${ENVIRONMENT}-${ACTIVE_ENV}"
    docker compose -p "$old_project_name" down
    
    # Success
    echo ""
    echo -e "${GREEN}=========================================${NC}"
    echo -e "${GREEN}BLUE-GREEN DEPLOYMENT SUCCESSFUL${NC}"
    echo -e "${GREEN}=========================================${NC}"
    echo "Active environment: ${INACTIVE_ENV}"
    echo "Version: ${VERSION}"
    echo "Deployment completed at: $(date)"
    echo ""
    
    # Log deployment
    log_deployment "$ENVIRONMENT" "$VERSION" "$INACTIVE_ENV" "blue-green" "success"
}

# Function to log deployment
log_deployment() {
    local env=$1
    local version=$2
    local target=$3
    local strategy=$4
    local status=$5
    
    local log_file="${PROJECT_ROOT}/deploy/deployment-audit.log"
    local timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    
    echo "${timestamp}|${env}|${version}|${target}|${strategy}|${status}|${USER:-system}" >> "$log_file"
}

# Run main deployment
main "$@"
