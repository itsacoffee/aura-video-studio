#!/bin/bash
# Automated Rollback Script
# Quickly reverts to previous stable version

set -euo pipefail

ENVIRONMENT="${1:-staging}"
REASON="${2:-Manual rollback}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${RED}=========================================${NC}"
echo -e "${RED}AUTOMATED ROLLBACK${NC}"
echo -e "${RED}=========================================${NC}"
echo "Environment: $ENVIRONMENT"
echo "Reason: $REASON"
echo "Timestamp: $(date)"
echo ""

# Get previous version from deployment history
get_previous_version() {
  local audit_log="${PROJECT_ROOT}/deploy/deployment-audit.log"

  if [ ! -f "$audit_log" ]; then
    echo "unknown"
    return
  fi

  # Get last successful deployment before current one
  local previous=$(grep "|${ENVIRONMENT}|.*|success|" "$audit_log" | tail -n 2 | head -n 1 | cut -d'|' -f3)

  if [ -z "$previous" ]; then
    echo "unknown"
  else
    echo "$previous"
  fi
}

# Get current deployment state
get_deployment_state() {
  local state_file="${PROJECT_ROOT}/deploy/.deployment-state-${ENVIRONMENT}"

  if [ -f "$state_file" ]; then
    cat "$state_file"
  else
    echo "unknown"
  fi
}

# Rollback blue-green deployment
rollback_blue_green() {
  local current_env=$1
  local previous_env

  if [ "$current_env" == "blue" ]; then
    previous_env="green"
  else
    previous_env="blue"
  fi

  echo -e "${BLUE}Rolling back blue-green deployment...${NC}"
  echo "Switching from ${current_env} to ${previous_env}"

  # Switch traffic back
  echo "Switching traffic to ${previous_env}..."

  # Update load balancer
  cat >"${PROJECT_ROOT}/deploy/nginx/upstream.conf" <<EOF
upstream aura_backend {
    server api_${previous_env}:5005;
}

upstream aura_frontend {
    server web_${previous_env}:80;
}
EOF

  # Reload nginx
  docker exec aura-loadbalancer nginx -s reload 2>/dev/null || true

  # Update state
  echo "$previous_env" >"${PROJECT_ROOT}/deploy/.deployment-state-${ENVIRONMENT}"

  echo -e "${GREEN}✓ Traffic switched to ${previous_env}${NC}"
}

# Rollback using Docker images
rollback_docker() {
  local previous_version=$1

  echo -e "${BLUE}Rolling back Docker deployment...${NC}"
  echo "Rolling back to version: ${previous_version}"

  # Export environment variables
  export VERSION="$previous_version"
  export DOCKER_REGISTRY="${DOCKER_REGISTRY:-ghcr.io/aura}"

  local compose_file="${PROJECT_ROOT}/docker-compose.prod.yml"
  local project_name="aura-${ENVIRONMENT}"

  # Stop current version
  echo "Stopping current version..."
  docker compose -f "$compose_file" -p "$project_name" down

  # Start previous version
  echo "Starting previous version ${previous_version}..."
  docker compose -f "$compose_file" -p "$project_name" up -d

  # Wait for services
  echo "Waiting for services to start..."
  sleep 30

  # Verify health
  if curl -sf "http://localhost:5005/health/live" >/dev/null; then
    echo -e "${GREEN}✓ Services healthy after rollback${NC}"
  else
    echo -e "${RED}✗ Warning: Services may not be fully healthy${NC}"
  fi
}

# Rollback database (if needed)
rollback_database() {
  echo -e "${BLUE}Checking database rollback requirements...${NC}"

  # Check if database backup exists
  local backup_file="${PROJECT_ROOT}/data/aura.db.backup"

  if [ -f "$backup_file" ]; then
    echo "Database backup found, rolling back..."

    # Stop services
    docker compose -p "aura-${ENVIRONMENT}" stop api

    # Restore database
    cp "$backup_file" "${PROJECT_ROOT}/data/aura.db"

    # Restart services
    docker compose -p "aura-${ENVIRONMENT}" start api

    echo -e "${GREEN}✓ Database rolled back${NC}"
  else
    echo "No database backup found, skipping database rollback"
  fi
}

# Verify rollback
verify_rollback() {
  echo ""
  echo -e "${BLUE}Verifying rollback...${NC}"

  local checks=0
  local max_checks=10

  while [ $checks -lt $max_checks ]; do
    checks=$((checks + 1))

    # Check API health
    if curl -sf "http://localhost:5005/health/system" >/dev/null; then
      echo -e "${GREEN}✓ API health check passed (attempt ${checks}/${max_checks})${NC}"

      # Check version
      local current_version=$(curl -s "http://localhost:5005/api/version" | jq -r '.version' 2>/dev/null || echo "unknown")
      echo "Current version: ${current_version}"

      return 0
    fi

    echo "Health check attempt ${checks}/${max_checks} failed"
    sleep 10
  done

  echo -e "${RED}✗ Rollback verification failed${NC}"
  return 1
}

# Log rollback
log_rollback() {
  local status=$1
  local audit_log="${PROJECT_ROOT}/deploy/rollback-audit.log"
  local timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

  echo "${timestamp}|${ENVIRONMENT}|${REASON}|${status}|${USER:-system}" >>"$audit_log"
}

# Send notification
send_notification() {
  local status=$1

  echo ""
  echo -e "${YELLOW}Sending rollback notification...${NC}"

  # Add notification logic here
  # Examples:
  # - Slack webhook
  # - Email
  # - PagerDuty
  # - Microsoft Teams

  cat <<EOF >/tmp/rollback-notification.json
{
  "environment": "${ENVIRONMENT}",
  "status": "${status}",
  "reason": "${REASON}",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "operator": "${USER:-system}"
}
EOF

  # Example: Send to Slack
  # curl -X POST -H 'Content-type: application/json' \
  #   --data @/tmp/rollback-notification.json \
  #   "${SLACK_WEBHOOK_URL}"

  echo "Notification prepared (configure webhook to send)"
}

# Main rollback process
main() {
  # Get current state
  CURRENT_STATE=$(get_deployment_state)
  PREVIOUS_VERSION=$(get_previous_version)

  echo "Current state: ${CURRENT_STATE}"
  echo "Previous version: ${PREVIOUS_VERSION}"
  echo ""

  # Determine rollback strategy
  if [ "$CURRENT_STATE" == "blue" ] || [ "$CURRENT_STATE" == "green" ]; then
    # Blue-green deployment
    rollback_blue_green "$CURRENT_STATE"
  elif [ "$PREVIOUS_VERSION" != "unknown" ]; then
    # Docker-based rollback
    rollback_docker "$PREVIOUS_VERSION"
  else
    echo -e "${RED}Unable to determine rollback strategy${NC}"
    log_rollback "failed"
    exit 1
  fi

  # Optional: Rollback database
  # rollback_database

  # Verify rollback
  if verify_rollback; then
    echo ""
    echo -e "${GREEN}=========================================${NC}"
    echo -e "${GREEN}ROLLBACK SUCCESSFUL${NC}"
    echo -e "${GREEN}=========================================${NC}"

    log_rollback "success"
    send_notification "success"
    exit 0
  else
    echo ""
    echo -e "${RED}=========================================${NC}"
    echo -e "${RED}ROLLBACK FAILED${NC}"
    echo -e "${RED}=========================================${NC}"

    log_rollback "failed"
    send_notification "failed"
    exit 1
  fi
}

# Trap errors
trap 'echo -e "${RED}Rollback script encountered an error${NC}"; log_rollback "error"; exit 1' ERR

# Run main process
main "$@"
