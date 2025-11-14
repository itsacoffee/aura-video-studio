#!/bin/bash
# Semantic Versioning Script
# Automatically determines next version based on conventional commits

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Get current version
get_current_version() {
  # Try to get from git tags
  local version=$(git describe --tags --abbrev=0 2>/dev/null || echo "")

  if [ -z "$version" ]; then
    # Fallback to version file
    if [ -f "${PROJECT_ROOT}/version.json" ]; then
      version=$(jq -r '.version' "${PROJECT_ROOT}/version.json")
    else
      echo "v0.0.0"
      return
    fi
  fi

  echo "$version"
}

# Parse semantic version
parse_version() {
  local version=$1
  version=${version#v} # Remove 'v' prefix

  local IFS='.'
  read -ra parts <<<"$version"

  MAJOR="${parts[0]:-0}"
  MINOR="${parts[1]:-0}"
  PATCH="${parts[2]:-0}"
}

# Analyze commits since last tag
analyze_commits() {
  local last_tag=$1
  local commit_range

  if [ -z "$last_tag" ] || [ "$last_tag" == "v0.0.0" ]; then
    commit_range="HEAD"
  else
    commit_range="${last_tag}..HEAD"
  fi

  # Get all commits
  local commits=$(git log --pretty=format:"%s" "$commit_range")

  # Check for breaking changes
  if echo "$commits" | grep -qiE "(BREAKING CHANGE|^[a-z]+!:)"; then
    echo "major"
    return
  fi

  # Check for new features
  if echo "$commits" | grep -qiE "^feat(\([a-z]+\))?:"; then
    echo "minor"
    return
  fi

  # Check for fixes
  if echo "$commits" | grep -qiE "^fix(\([a-z]+\))?:"; then
    echo "patch"
    return
  fi

  # Default to patch if there are any commits
  if [ -n "$commits" ]; then
    echo "patch"
  else
    echo "none"
  fi
}

# Calculate next version
calculate_next_version() {
  local current=$1
  local bump_type=$2

  parse_version "$current"

  case "$bump_type" in
    major)
      MAJOR=$((MAJOR + 1))
      MINOR=0
      PATCH=0
      ;;
    minor)
      MINOR=$((MINOR + 1))
      PATCH=0
      ;;
    patch)
      PATCH=$((PATCH + 1))
      ;;
    *)
      echo "$current"
      return
      ;;
  esac

  echo "v${MAJOR}.${MINOR}.${PATCH}"
}

# Update version files
update_version_files() {
  local version=$1
  local clean_version=${version#v}

  echo -e "${BLUE}Updating version files to ${version}...${NC}"

  # Update version.json
  cat >"${PROJECT_ROOT}/version.json" <<EOF
{
  "version": "${clean_version}",
  "buildDate": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "gitCommit": "$(git rev-parse --short HEAD)"
}
EOF

  echo -e "${GREEN}✓ Updated version.json${NC}"

  # Update .csproj files
  find "$PROJECT_ROOT" -name "*.csproj" -type f | while read -r csproj; do
    if grep -q "<Version>" "$csproj"; then
      sed -i.bak "s/<Version>.*<\/Version>/<Version>${clean_version}<\/Version>/" "$csproj"
      rm "${csproj}.bak"
      echo -e "${GREEN}✓ Updated $(basename $csproj)${NC}"
    fi
  done

  # Update package.json files
  find "$PROJECT_ROOT" -name "package.json" -type f | while read -r package; do
    if [ "$(dirname $package)" != "${PROJECT_ROOT}/node_modules" ]; then
      jq ".version = \"${clean_version}\"" "$package" >"${package}.tmp"
      mv "${package}.tmp" "$package"
      echo -e "${GREEN}✓ Updated $(basename $(dirname $package))/package.json${NC}"
    fi
  done
}

# Main function
main() {
  cd "$PROJECT_ROOT"

  echo -e "${BLUE}=========================================${NC}"
  echo -e "${BLUE}Semantic Version Calculator${NC}"
  echo -e "${BLUE}=========================================${NC}"
  echo ""

  # Get current version
  CURRENT_VERSION=$(get_current_version)
  echo "Current version: ${CURRENT_VERSION}"

  # Analyze commits
  echo ""
  echo "Analyzing commits since ${CURRENT_VERSION}..."
  BUMP_TYPE=$(analyze_commits "$CURRENT_VERSION")

  if [ "$BUMP_TYPE" == "none" ]; then
    echo -e "${YELLOW}No version bump needed (no commits since last tag)${NC}"
    exit 0
  fi

  echo "Bump type: ${BUMP_TYPE}"

  # Calculate next version
  NEXT_VERSION=$(calculate_next_version "$CURRENT_VERSION" "$BUMP_TYPE")

  echo ""
  echo -e "${GREEN}Current version: ${CURRENT_VERSION}${NC}"
  echo -e "${GREEN}Next version:    ${NEXT_VERSION}${NC}"
  echo -e "${GREEN}Bump type:       ${BUMP_TYPE}${NC}"

  # Update version files
  if [ "${1:-}" != "--dry-run" ]; then
    echo ""
    update_version_files "$NEXT_VERSION"

    echo ""
    echo -e "${GREEN}=========================================${NC}"
    echo -e "${GREEN}Version updated to ${NEXT_VERSION}${NC}"
    echo -e "${GREEN}=========================================${NC}"
    echo ""
    echo "Next steps:"
    echo "  1. Review the changes: git diff"
    echo "  2. Commit the changes: git add . && git commit -m 'chore: bump version to ${NEXT_VERSION}'"
    echo "  3. Create a tag: git tag ${NEXT_VERSION}"
    echo "  4. Push: git push && git push --tags"
  else
    echo ""
    echo -e "${YELLOW}Dry run - no files were modified${NC}"
  fi
}

# Run main
main "$@"
