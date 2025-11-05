#!/bin/bash
# CI check script to ensure no plaintext API key files are committed
# Part of security enforcement for issue #231 (secrets encryption alignment)

set -e

echo "🔍 Checking for plaintext API key files..."

# Define patterns that should not exist in the repository
FORBIDDEN_PATHS=(
    ".aura-dev/apikeys.json"
    ".aura-dev/api-keys.json"
    "**/apikeys.json"
    "**/api-keys.json"
    "**/keys.json"
    "**/secrets.json"
    "Aura/apikeys.json"
    "%LOCALAPPDATA%/Aura/apikeys.json"
    "**/*.backup"
    "**/*.bak"
)

EXCLUDED_PATHS=(
    "docs/"
    "examples/"
    "*.md"
    "test/"
    "tests/"
)

# Track if any forbidden files are found
FOUND_VIOLATIONS=0

# Check for plaintext key files using git ls-files
for pattern in "${FORBIDDEN_PATHS[@]}"; do
    echo "  Checking pattern: $pattern"
    
    # Use git ls-files to find tracked files matching the pattern
    # Pattern is properly quoted to prevent shell injection
    FILES=$(git ls-files -- "$pattern" 2>/dev/null || true)
    
    if [ -n "$FILES" ]; then
        while IFS= read -r file; do
            # Check if file should be excluded
            EXCLUDED=0
            for exclude_pattern in "${EXCLUDED_PATHS[@]}"; do
                # Use proper pattern matching with quoted variables
                if [[ "$file" == "$exclude_pattern"* ]]; then
                    EXCLUDED=1
                    break
                fi
            done
            
            if [ $EXCLUDED -eq 0 ]; then
                echo "  ❌ VIOLATION: Found plaintext key file: $file"
                FOUND_VIOLATIONS=$((FOUND_VIOLATIONS + 1))
            fi
        done <<< "$FILES"
    fi
done

# Check for sensitive content patterns in tracked files
echo "  Checking for exposed API keys in files..."
SENSITIVE_PATTERNS=(
    "sk-proj-[A-Za-z0-9]"
    "sk-ant-api[0-9]"
    "AIza[A-Za-z0-9]"
    "el_[A-Za-z0-9]"
    "r8_[A-Za-z0-9]"
    "eyJ[A-Za-z0-9_-]+\\.eyJ"        # JWT tokens
    "Bearer [A-Za-z0-9_-]{20,}"     # Bearer tokens
    "x-api-key: [A-Za-z0-9_-]{16,}" # x-api-key headers
)

# Search only in specific file types to avoid false positives
SEARCH_EXTENSIONS="*.cs *.ts *.tsx *.js *.jsx *.json"

for pattern in "${SENSITIVE_PATTERNS[@]}"; do
    # Search in tracked files, excluding test files and docs
    MATCHES=$(git grep -l -E "$pattern" -- $SEARCH_EXTENSIONS ':(exclude)*/test/*' ':(exclude)*/tests/*' ':(exclude)**/__tests__/*' ':(exclude)*.test.*' ':(exclude)Aura.Tests/' ':(exclude)Aura.E2E/' ':(exclude)docs/' ':(exclude)examples/' ':(exclude)*.md' 2>/dev/null || true)
    
    if [ -n "$MATCHES" ]; then
        echo "  ⚠️  WARNING: Found potential API key pattern '$pattern' in:"
        echo "$MATCHES" | while read -r file; do
            echo "      - $file"
        done
        # Don't fail on pattern matches, just warn (could be test data or examples)
    fi
done

# Check for backup files that might contain secrets
echo "  Checking for backup files..."
BACKUP_FILES=$(git ls-files | grep -E '\.(backup|bak)$|_(backup|bak)$' | grep -v -E '^(docs|examples|tests)/' || true)
if [ -n "$BACKUP_FILES" ]; then
    echo "  ❌ VIOLATION: Found backup files:"
    echo "$BACKUP_FILES" | while IFS= read -r file; do
        echo "      - $file"
    done
    FOUND_VIOLATIONS=$((FOUND_VIOLATIONS + 1))
fi

# Report results
echo ""
if [ $FOUND_VIOLATIONS -eq 0 ]; then
    echo "✅ No plaintext API key files found"
    exit 0
else
    echo "❌ Found $FOUND_VIOLATIONS plaintext API key file(s)"
    echo ""
    echo "API keys must be stored in encrypted format:"
    echo "  - Windows: DPAPI-encrypted in apikeys.json"
    echo "  - Linux/macOS: AES-256 encrypted in secure/apikeys.dat"
    echo ""
    echo "Please remove these files and use the KeyStore API for secrets management."
    exit 1
fi
