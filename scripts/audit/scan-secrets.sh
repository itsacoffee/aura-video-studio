#!/bin/bash
# CI secrets scanning script to prevent sensitive data leakage
# Detects API keys, tokens, backup files, and other security violations

set -e

echo "🔒 Scanning for secrets and sensitive patterns..."

# Track violations
FOUND_VIOLATIONS=0
VIOLATIONS_FILE="/tmp/aura-secrets-violations-$$.txt"
: >"$VIOLATIONS_FILE"

# Colors for output
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

# Secret patterns to detect in file contents
# These patterns match common API keys and tokens
SECRET_PATTERNS=(
  'sk-[a-zA-Z0-9_-]{20,}'                                   # OpenAI/Anthropic API keys
  'sk-proj-[a-zA-Z0-9_-]{20,}'                              # OpenAI project keys
  'sk-ant-api[0-9]{2}-[a-zA-Z0-9_-]{20,}'                   # Anthropic API keys
  '["\x27]api_key["\x27]\s*:\s*["\x27][^"\x27]{16,}["\x27]' # Generic api_key in JSON
  '["\x27]apiKey["\x27]\s*:\s*["\x27][^"\x27]{16,}["\x27]'  # Generic apiKey in JSON
  'x-api-key:\s*[a-zA-Z0-9_-]{16,}'                         # x-api-key header
  'Authorization:\s*Bearer\s+[a-zA-Z0-9_.-]{20,}'           # Bearer tokens
  'Bearer\s+eyJ[a-zA-Z0-9_.-]+'                             # JWT tokens (Bearer eyJ...)
  '^eyJ[a-zA-Z0-9_-]+\.eyJ[a-zA-Z0-9_-]+\.'                 # Raw JWT tokens
  'AIza[a-zA-Z0-9_-]{35}'                                   # Google API keys
  'ya29\.[a-zA-Z0-9_-]{100,}'                               # Google OAuth tokens
  '[0-9]+-[a-zA-Z0-9_-]{32}\.apps\.googleusercontent\.com'  # Google OAuth client IDs
  'AKIA[0-9A-Z]{16}'                                        # AWS access key IDs
  'r8_[a-zA-Z0-9]{40,}'                                     # Replicate API keys
  'ghp_[a-zA-Z0-9]{36,}'                                    # GitHub personal access tokens
  'gho_[a-zA-Z0-9]{36,}'                                    # GitHub OAuth tokens
  'ghs_[a-zA-Z0-9]{36,}'                                    # GitHub app tokens
)

# Forbidden filenames - these should never be committed
FORBIDDEN_FILENAMES=(
  'apikeys.json'
  'api-keys.json'
  'keys.json'
  'secrets.json'
  '.env.production'
  '.env.prod'
  '*.backup'
  '*.bak'
  '*_backup'
  '*_bak'
)

# Directories to exclude from scanning
EXCLUDE_PATHS=(
  'node_modules/'
  'dist/'
  'build/'
  'bin/'
  'obj/'
  '.git/'
  'coverage/'
  'docs/'
  'examples/'
  '*.md'
  'tests/'
  'test/'
  '*/__tests__/*'
  '*.test.ts'
  '*.test.tsx'
  '*.test.js'
  '*.test.jsx'
  'Aura.Tests/'
  'Aura.E2E/'
)

# File extensions to scan for secret patterns
SCAN_EXTENSIONS="*.cs *.ts *.tsx *.js *.jsx *.json *.yml *.yaml *.txt *.log *.config"

echo "📋 Step 1: Checking for forbidden filenames..."

# Build git pathspec excludes
PATHSPEC_EXCLUDES=""
for exclude in "${EXCLUDE_PATHS[@]}"; do
  PATHSPEC_EXCLUDES="$PATHSPEC_EXCLUDES :(exclude)$exclude"
done

# Check for forbidden filenames
for pattern in "${FORBIDDEN_FILENAMES[@]}"; do
  FILES=$(git ls-files -- "$pattern" $PATHSPEC_EXCLUDES 2>/dev/null || true)

  if [ -n "$FILES" ]; then
    while IFS= read -r file; do
      echo "  ${RED}✗${NC} VIOLATION: Forbidden file found: $file"
      echo "FORBIDDEN_FILE: $file" >>"$VIOLATIONS_FILE"
      FOUND_VIOLATIONS=$((FOUND_VIOLATIONS + 1))
    done <<<"$FILES"
  fi
done

echo "📋 Step 2: Scanning file contents for secret patterns..."

# Scan for secret patterns in tracked files
for pattern in "${SECRET_PATTERNS[@]}"; do
  # Use git grep with perl regex for more advanced patterns
  MATCHES=$(git grep -P -n -I "$pattern" -- $SCAN_EXTENSIONS $PATHSPEC_EXCLUDES ':(exclude)**/__tests__/*' ':(exclude)*.test.*' ':(exclude)Aura.Tests/' ':(exclude)Aura.E2E/' 2>/dev/null || true)

  if [ -n "$MATCHES" ]; then
    echo "  ${RED}✗${NC} Found potential secret pattern: ${pattern:0:30}..."
    echo "$MATCHES" | while IFS= read -r match; do
      file=$(echo "$match" | cut -d':' -f1)
      line=$(echo "$match" | cut -d':' -f2)
      echo "    - $file:$line"
      echo "SECRET_PATTERN: $file:$line" >>"$VIOLATIONS_FILE"
    done
    FOUND_VIOLATIONS=$((FOUND_VIOLATIONS + 1))
  fi
done

echo "📋 Step 3: Checking for backup files in source directories..."

# Look for backup files that might contain sensitive data
BACKUP_FILES=$(git ls-files | grep -E '\.(backup|bak)$|_(backup|bak)$' | grep -v -E '^(docs|examples|tests)/' || true)

if [ -n "$BACKUP_FILES" ]; then
  echo "  ${RED}✗${NC} Found backup files:"
  echo "$BACKUP_FILES" | while IFS= read -r file; do
    echo "    - $file"
    echo "BACKUP_FILE: $file" >>"$VIOLATIONS_FILE"
    FOUND_VIOLATIONS=$((FOUND_VIOLATIONS + 1))
  done
fi

echo "📋 Step 4: Checking for plaintext key files (enhanced check)..."

# Check for files that look like they contain keys based on content
POTENTIAL_KEY_FILES=$(git grep -l -i -E '(api[_-]?key|secret|password|token|bearer|authorization)' -- '*.json' '*.config' $PATHSPEC_EXCLUDES 2>/dev/null || true)

if [ -n "$POTENTIAL_KEY_FILES" ]; then
  while IFS= read -r file; do
    # Skip known safe files
    if [[ "$file" =~ (package\.json|package-lock\.json|tsconfig\.json|appsettings\.json|Directory\.Build\.props) ]]; then
      continue
    fi

    # Check if file contains actual key-value pairs that look suspicious
    if git show "HEAD:$file" | grep -q -E '["\x27](api_key|apiKey|secret|password|token)["\x27]\s*:\s*["\x27][^"\x27]{16,}["\x27]' 2>/dev/null; then
      echo "  ${YELLOW}⚠${NC}  WARNING: Potential secrets in: $file"
      echo "POTENTIAL_SECRETS: $file" >>"$VIOLATIONS_FILE"
    fi
  done <<<"$POTENTIAL_KEY_FILES"
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Generate report
if [ $FOUND_VIOLATIONS -eq 0 ] && [ ! -s "$VIOLATIONS_FILE" ]; then
  echo "${GREEN}✓${NC} No secrets or sensitive patterns detected"
  rm -f "$VIOLATIONS_FILE"
  exit 0
else
  echo "${RED}✗ Found $FOUND_VIOLATIONS security violation(s)${NC}"
  echo ""
  echo "Violations detected:"
  cat "$VIOLATIONS_FILE" | head -20

  if [ $(wc -l <"$VIOLATIONS_FILE") -gt 20 ]; then
    echo "... and $(($(wc -l <"$VIOLATIONS_FILE") - 20)) more"
  fi

  echo ""
  echo "Security Guidelines:"
  echo "  • Never commit API keys, tokens, or passwords"
  echo "  • Use environment variables or encrypted key stores"
  echo "  • Remove backup files (*.backup, *.bak) before committing"
  echo "  • Ensure .gitignore excludes sensitive files"
  echo ""
  echo "For secrets management, see: SECURITY.md"

  rm -f "$VIOLATIONS_FILE"
  exit 1
fi
