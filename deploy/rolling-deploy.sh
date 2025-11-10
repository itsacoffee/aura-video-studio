#!/bin/bash
# Rolling Deployment Script
# Gradually replaces instances with new version

set -euo pipefail

ENVIRONMENT="${1:-staging}"
VERSION="${2:-latest}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=========================================${NC}"
echo -e "${BLUE}Rolling Deployment${NC}"
echo -e "${BLUE}=========================================${NC}"
echo "Environment: $ENVIRONMENT"
echo "Version: $VERSION"
echo ""

# Configuration
INSTANCES=3  # Number of instances to deploy
BATCH_SIZE=1  # Deploy one instance at a time
HEALTH_CHECK_DELAY=30  # seconds between health checks
MAX_HEALTH_CHECKS=10

# Function to deploy instance
deploy_instance() {
    local instance_id=$1
    local version=$2
    
    echo ""
    echo -e "${BLUE}Deploying instance ${instance_id}...${NC}"
    
    # Stop old instance
    echo "Stopping old instance ${instance_id}..."
    docker stop "aura-api-${instance_id}" 2>/dev/null || true
    docker rm "aura-api-${instance_id}" 2>/dev/null || true
    
    # Start new instance
    echo "Starting new instance ${instance_id} with version ${version}..."
    docker run -d \
        --name "aura-api-${instance_id}" \
        --network aura-network \
        -e ASPNETCORE_ENVIRONMENT=Production \
        -e VERSION="${version}" \
        "${DOCKER_REGISTRY:-ghcr.io/aura}/aura-api:${version}"
    
    # Wait for instance to be ready
    local port=$((5005 + instance_id))
    local health_url="http://localhost:${port}/health/live"
    local checks=0
    
    echo "Waiting for instance ${instance_id} to be healthy..."
    while [ $checks -lt $MAX_HEALTH_CHECKS ]; do
        if curl -sf "$health_url" > /dev/null 2>&1; then
            echo -e "${GREEN}✓ Instance ${instance_id} is healthy${NC}"
            return 0
        fi
        
        checks=$((checks + 1))
        sleep $HEALTH_CHECK_DELAY
    done
    
    echo -e "${RED}✗ Instance ${instance_id} failed health checks${NC}"
    return 1
}

# Function to rollback instance
rollback_instance() {
    local instance_id=$1
    
    echo -e "${YELLOW}Rolling back instance ${instance_id}...${NC}"
    
    docker stop "aura-api-${instance_id}" 2>/dev/null || true
    docker rm "aura-api-${instance_id}" 2>/dev/null || true
    
    # Restart with previous version
    docker start "aura-api-${instance_id}-backup" 2>/dev/null || true
}

# Main deployment
main() {
    local deployed=0
    local failed=false
    
    for instance in $(seq 1 $INSTANCES); do
        # Create backup
        echo "Creating backup of instance ${instance}..."
        docker rename "aura-api-${instance}" "aura-api-${instance}-backup" 2>/dev/null || true
        
        # Deploy new instance
        if deploy_instance "$instance" "$VERSION"; then
            deployed=$((deployed + 1))
            echo -e "${GREEN}✓ Successfully deployed instance ${instance}${NC}"
            
            # Wait before deploying next instance
            if [ $instance -lt $INSTANCES ]; then
                echo "Waiting ${HEALTH_CHECK_DELAY}s before deploying next instance..."
                sleep $HEALTH_CHECK_DELAY
            fi
        else
            echo -e "${RED}✗ Failed to deploy instance ${instance}${NC}"
            failed=true
            break
        fi
    done
    
    if [ "$failed" = true ]; then
        echo ""
        echo -e "${RED}Rolling deployment failed. Initiating rollback...${NC}"
        
        for instance in $(seq 1 $deployed); do
            rollback_instance "$instance"
        done
        
        exit 1
    fi
    
    # Cleanup backups
    echo ""
    echo "Cleaning up backups..."
    for instance in $(seq 1 $INSTANCES); do
        docker stop "aura-api-${instance}-backup" 2>/dev/null || true
        docker rm "aura-api-${instance}-backup" 2>/dev/null || true
    done
    
    echo ""
    echo -e "${GREEN}=========================================${NC}"
    echo -e "${GREEN}ROLLING DEPLOYMENT SUCCESSFUL${NC}"
    echo -e "${GREEN}=========================================${NC}"
    echo "Deployed ${deployed}/${INSTANCES} instances"
    echo "Version: ${VERSION}"
}

main "$@"
