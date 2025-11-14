#!/bin/bash
# Deployment Audit Script
# Logs deployment information for compliance and audit trails

set -euo pipefail

ENVIRONMENT="${1:-staging}"
VERSION="${2:-unknown}"
STRATEGY="${3:-manual}"
STATUS="${4:-initiated}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

AUDIT_LOG="${PROJECT_ROOT}/deploy/deployment-audit.log"
AUDIT_JSON="${PROJECT_ROOT}/deploy/audit-records"

# Ensure audit directories exist
mkdir -p "$AUDIT_JSON"

# Generate unique deployment ID
DEPLOYMENT_ID=$(date +%Y%m%d_%H%M%S)_$$

# Collect deployment metadata
collect_metadata() {
  local metadata=""

  # System information
  metadata+="operator=$(whoami || echo 'unknown'),"
  metadata+="hostname=$(hostname || echo 'unknown'),"
  metadata+="timestamp=$(date -u +%Y-%m-%dT%H:%M:%SZ),"

  # Git information
  if command -v git &>/dev/null; then
    metadata+="git_branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo 'unknown'),"
    metadata+="git_commit=$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown'),"
    metadata+="git_remote=$(git config --get remote.origin.url 2>/dev/null || echo 'unknown'),"
  fi

  # CI/CD information
  if [ -n "${GITHUB_RUN_ID:-}" ]; then
    metadata+="ci=github,"
    metadata+="run_id=${GITHUB_RUN_ID},"
    metadata+="run_number=${GITHUB_RUN_NUMBER:-unknown},"
    metadata+="actor=${GITHUB_ACTOR:-unknown},"
  elif [ -n "${GITLAB_CI:-}" ]; then
    metadata+="ci=gitlab,"
    metadata+="pipeline_id=${CI_PIPELINE_ID:-unknown},"
    metadata+="job_id=${CI_JOB_ID:-unknown},"
  fi

  echo "$metadata"
}

# Log to structured audit log
log_audit() {
  local metadata=$(collect_metadata)

  # Append to CSV-style audit log
  echo "${DEPLOYMENT_ID}|${ENVIRONMENT}|${VERSION}|${STRATEGY}|${STATUS}|${metadata}" >>"$AUDIT_LOG"

  # Create JSON audit record
  cat >"${AUDIT_JSON}/${DEPLOYMENT_ID}.json" <<EOF
{
  "deploymentId": "${DEPLOYMENT_ID}",
  "environment": "${ENVIRONMENT}",
  "version": "${VERSION}",
  "strategy": "${STRATEGY}",
  "status": "${STATUS}",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "operator": "$(whoami || echo 'unknown')",
  "hostname": "$(hostname || echo 'unknown')",
  "git": {
    "branch": "$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo 'unknown')",
    "commit": "$(git rev-parse HEAD 2>/dev/null || echo 'unknown')",
    "shortCommit": "$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown')"
  },
  "ci": {
    "provider": "${GITHUB_RUN_ID:+github}${GITLAB_CI:+gitlab}",
    "runId": "${GITHUB_RUN_ID:-${CI_PIPELINE_ID:-unknown}}",
    "runNumber": "${GITHUB_RUN_NUMBER:-${CI_PIPELINE_IID:-unknown}}",
    "actor": "${GITHUB_ACTOR:-${GITLAB_USER_LOGIN:-$(whoami)}}"
  },
  "metadata": {
    "approver": "${DEPLOYMENT_APPROVER:-}",
    "changeTicket": "${CHANGE_TICKET:-}",
    "rollbackVersion": "${ROLLBACK_VERSION:-}",
    "notes": "${DEPLOYMENT_NOTES:-}"
  }
}
EOF

  echo "$DEPLOYMENT_ID"
}

# Update deployment status
update_status() {
  local deployment_id=$1
  local new_status=$2
  local details="${3:-}"

  local record_file="${AUDIT_JSON}/${deployment_id}.json"

  if [ -f "$record_file" ]; then
    # Update JSON record
    local temp_file=$(mktemp)
    jq ".status = \"${new_status}\" | .completedAt = \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\" | .details = \"${details}\"" "$record_file" >"$temp_file"
    mv "$temp_file" "$record_file"
  fi
}

# Generate audit report
generate_report() {
  local output_file="${1:-/tmp/deployment-audit-report.md}"
  local days="${2:-30}"

  echo "# Deployment Audit Report"
  echo ""
  echo "Report Generated: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "Period: Last ${days} days"
  echo ""

  # Count deployments by environment
  echo "## Deployments by Environment"
  echo ""
  echo "| Environment | Count |"
  echo "|------------|-------|"

  for env in staging production; do
    local count=$(grep -c "|${env}|" "$AUDIT_LOG" 2>/dev/null || echo "0")
    echo "| ${env} | ${count} |"
  done

  echo ""

  # Recent deployments
  echo "## Recent Deployments"
  echo ""
  echo "| Date | Environment | Version | Strategy | Status |"
  echo "|------|-------------|---------|----------|--------|"

  tail -n 20 "$AUDIT_LOG" | while IFS='|' read -r id env ver strat stat meta; do
    local timestamp=$(echo "$meta" | grep -oP 'timestamp=\K[^,]+' || echo "unknown")
    echo "| ${timestamp} | ${env} | ${ver} | ${strat} | ${stat} |"
  done

  echo ""

  # Success/Failure statistics
  echo "## Success Rate"
  echo ""

  local total=$(wc -l <"$AUDIT_LOG" 2>/dev/null || echo "0")
  local successful=$(grep -c "success" "$AUDIT_LOG" 2>/dev/null || echo "0")
  local failed=$(grep -c "failed" "$AUDIT_LOG" 2>/dev/null || echo "0")

  if [ "$total" -gt 0 ]; then
    local success_rate=$((successful * 100 / total))
    echo "- Total deployments: ${total}"
    echo "- Successful: ${successful} (${success_rate}%)"
    echo "- Failed: ${failed}"
  else
    echo "- No deployments recorded"
  fi

  echo ""
} >"$output_file"

# Main command handling
case "${5:-log}" in
  log)
    log_audit
    ;;
  update)
    update_status "$DEPLOYMENT_ID" "$STATUS" "${6:-}"
    ;;
  report)
    generate_report "${6:-deployment-audit-report.md}" "${7:-30}"
    echo "Report generated: ${6:-deployment-audit-report.md}"
    ;;
  *)
    echo "Usage: $0 <environment> <version> <strategy> <status> [log|update|report]"
    exit 1
    ;;
esac
