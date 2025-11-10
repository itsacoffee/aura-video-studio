#!/bin/bash
# Changelog Generation Script
# Generates CHANGELOG.md from conventional commits

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

# Get commits between tags
get_commits() {
    local from_tag=$1
    local to_tag=${2:-HEAD}
    
    if [ -z "$from_tag" ]; then
        # Get all commits
        git log --pretty=format:"%H|%s|%b|%an|%aI" "$to_tag"
    else
        git log --pretty=format:"%H|%s|%b|%an|%aI" "${from_tag}..${to_tag}"
    fi
}

# Parse conventional commit
parse_commit() {
    local commit=$1
    
    IFS='|' read -r hash subject body author date <<< "$commit"
    
    # Parse conventional commit format
    if [[ $subject =~ ^([a-z]+)(\([a-z0-9_-]+\))?(!)?:\ (.+)$ ]]; then
        local type="${BASH_REMATCH[1]}"
        local scope="${BASH_REMATCH[2]}"
        local breaking="${BASH_REMATCH[3]}"
        local description="${BASH_REMATCH[4]}"
        
        scope=${scope#(}
        scope=${scope%)}
        
        echo "${type}|${scope}|${breaking}|${description}|${hash}|${author}|${date}"
    fi
}

# Group commits by type
group_commits() {
    local commits=$1
    
    declare -A groups
    groups["feat"]=""
    groups["fix"]=""
    groups["perf"]=""
    groups["refactor"]=""
    groups["docs"]=""
    groups["style"]=""
    groups["test"]=""
    groups["chore"]=""
    groups["ci"]=""
    groups["breaking"]=""
    
    while IFS= read -r commit; do
        local parsed=$(parse_commit "$commit")
        
        if [ -n "$parsed" ]; then
            IFS='|' read -r type scope breaking description hash author date <<< "$parsed"
            
            if [ -n "$breaking" ] || echo "$commit" | grep -q "BREAKING CHANGE"; then
                groups["breaking"]+="- ${description} (${hash:0:7})\n"
            fi
            
            if [ -n "${groups[$type]+x}" ]; then
                if [ -n "$scope" ]; then
                    groups["$type"]+="- **${scope}**: ${description} (${hash:0:7})\n"
                else
                    groups["$type"]+="- ${description} (${hash:0:7})\n"
                fi
            fi
        fi
    done <<< "$commits"
    
    # Print grouped commits
    if [ -n "${groups[breaking]}" ]; then
        echo -e "### ⚠️ BREAKING CHANGES\n\n${groups[breaking]}"
    fi
    
    if [ -n "${groups[feat]}" ]; then
        echo -e "### ✨ Features\n\n${groups[feat]}"
    fi
    
    if [ -n "${groups[fix]}" ]; then
        echo -e "### 🐛 Bug Fixes\n\n${groups[fix]}"
    fi
    
    if [ -n "${groups[perf]}" ]; then
        echo -e "### ⚡ Performance Improvements\n\n${groups[perf]}"
    fi
    
    if [ -n "${groups[refactor]}" ]; then
        echo -e "### ♻️ Code Refactoring\n\n${groups[refactor]}"
    fi
    
    if [ -n "${groups[docs]}" ]; then
        echo -e "### 📝 Documentation\n\n${groups[docs]}"
    fi
    
    if [ -n "${groups[test]}" ]; then
        echo -e "### ✅ Tests\n\n${groups[test]}"
    fi
    
    if [ -n "${groups[ci]}" ]; then
        echo -e "### 👷 CI/CD\n\n${groups[ci]}"
    fi
}

# Generate changelog
generate_changelog() {
    local version=$1
    local previous_tag=$2
    
    echo -e "${BLUE}Generating changelog for ${version}...${NC}"
    
    local commits=$(get_commits "$previous_tag")
    
    if [ -z "$commits" ]; then
        echo "No commits found between ${previous_tag} and HEAD"
        return
    fi
    
    # Create temporary changelog
    local temp_file=$(mktemp)
    
    {
        echo "# Changelog"
        echo ""
        echo "All notable changes to this project will be documented in this file."
        echo ""
        echo "The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),"
        echo "and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)."
        echo ""
        echo "## [${version}] - $(date +%Y-%m-%d)"
        echo ""
        group_commits "$commits"
        echo ""
        
        # Append existing changelog if it exists
        if [ -f "${PROJECT_ROOT}/CHANGELOG.md" ]; then
            # Skip the first 7 lines (header)
            tail -n +8 "${PROJECT_ROOT}/CHANGELOG.md"
        fi
    } > "$temp_file"
    
    # Replace changelog
    mv "$temp_file" "${PROJECT_ROOT}/CHANGELOG.md"
    
    echo -e "${GREEN}✓ Changelog generated${NC}"
}

# Main function
main() {
    cd "$PROJECT_ROOT"
    
    local version=${1:-$(git describe --tags --abbrev=0 2>/dev/null || echo "Unreleased")}
    local previous_tag=$(git describe --tags --abbrev=0 HEAD^ 2>/dev/null || echo "")
    
    echo -e "${BLUE}=========================================${NC}"
    echo -e "${BLUE}Changelog Generator${NC}"
    echo -e "${BLUE}=========================================${NC}"
    echo "Version: ${version}"
    echo "Previous tag: ${previous_tag:-none}"
    echo ""
    
    generate_changelog "$version" "$previous_tag"
    
    echo ""
    echo -e "${GREEN}Changelog generated at: ${PROJECT_ROOT}/CHANGELOG.md${NC}"
}

# Run main
main "$@"
