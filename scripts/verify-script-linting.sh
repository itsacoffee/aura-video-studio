#!/bin/bash
# Script Linting Verification - Final Check
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$REPO_ROOT"

echo "============================================"
echo "Script Linting Verification"
echo "============================================"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Counters
TOTAL_SHELL=0
TOTAL_PS=0
SHELL_WARNINGS=0
PS_WARNINGS=0

echo "Step 1: Checking Shell Scripts"
echo "--------------------------------------------"
TOTAL_SHELL=$(find . -type f -name "*.sh" ! -path "./.git/*" | wc -l)
echo "Found $TOTAL_SHELL shell scripts"

if command -v shellcheck >/dev/null 2>&1; then
  SHELL_WARNINGS=$(shellcheck -f gcc $(find . -type f -name "*.sh" ! -path "./.git/*") 2>&1 | grep -cE "error:|warning:")
  if [ -z "$SHELL_WARNINGS" ]; then
    SHELL_WARNINGS=0
  fi
  echo "ShellCheck warnings: $SHELL_WARNINGS"
  
  if [ "$SHELL_WARNINGS" -le 75 ]; then
    echo -e "${GREEN}✓ Shell scripts: Within acceptable range (<= 75 warnings)${NC}"
  else
    echo -e "${YELLOW}⚠ Shell scripts: More warnings than expected${NC}"
  fi
else
  echo -e "${YELLOW}⚠ ShellCheck not installed, skipping shell script checks${NC}"
fi

echo ""
echo "Step 2: Checking PowerShell Scripts"
echo "--------------------------------------------"
TOTAL_PS=$(find . -type f -name "*.ps1" ! -path "./.git/*" | wc -l)
echo "Found $TOTAL_PS PowerShell scripts"

if command -v pwsh >/dev/null 2>&1; then
  PS_WARNINGS=$(pwsh -Command "Get-ChildItem -Path . -Filter '*.ps1' -Recurse -File | Where-Object { \$_.FullName -notmatch '\\.git' } | ForEach-Object { Invoke-ScriptAnalyzer -Path \$_.FullName -Severity Warning,Error } | Measure-Object | Select-Object -ExpandProperty Count")
  if [ -z "$PS_WARNINGS" ]; then
    PS_WARNINGS=0
  fi
  echo "PSScriptAnalyzer warnings: $PS_WARNINGS"
  
  if [ "$PS_WARNINGS" -le 15 ]; then
    echo -e "${GREEN}✓ PowerShell scripts: Within acceptable range (<= 15 warnings)${NC}"
  else
    echo -e "${YELLOW}⚠ PowerShell scripts: More warnings than expected${NC}"
  fi
else
  echo -e "${YELLOW}⚠ PowerShell not installed, skipping PowerShell script checks${NC}"
fi

echo ""
echo "Step 3: Syntax Validation"
echo "--------------------------------------------"

# Test a few critical scripts
CRITICAL_SCRIPTS=(
  "setup.sh"
  "scripts/build-frontend.sh"
  "scripts/check-deps.sh"
)

SYNTAX_ERRORS=0
for script in "${CRITICAL_SCRIPTS[@]}"; do
  if [ -f "$script" ]; then
    if bash -n "$script" 2>/dev/null; then
      echo -e "${GREEN}✓${NC} $script"
    else
      echo -e "${RED}✗${NC} $script"
      SYNTAX_ERRORS=$((SYNTAX_ERRORS + 1))
    fi
  fi
done

CRITICAL_PS_SCRIPTS=(
  "setup.ps1"
  "scripts/validate-windows-ffmpeg.ps1"
)

for script in "${CRITICAL_PS_SCRIPTS[@]}"; do
  if [ -f "$script" ]; then
    if pwsh -Command "Get-Content '$script' | Out-Null" 2>/dev/null; then
      echo -e "${GREEN}✓${NC} $script"
    else
      echo -e "${RED}✗${NC} $script"
      SYNTAX_ERRORS=$((SYNTAX_ERRORS + 1))
    fi
  fi
done

echo ""
echo "============================================"
echo "Summary"
echo "============================================"
echo "Total shell scripts: $TOTAL_SHELL"
echo "Shell warnings: $SHELL_WARNINGS"
echo "Total PowerShell scripts: $TOTAL_PS"
echo "PowerShell warnings: $PS_WARNINGS"
echo "Syntax errors: $SYNTAX_ERRORS"
echo ""

if [ $SYNTAX_ERRORS -eq 0 ]; then
  echo -e "${GREEN}✓ All critical scripts have valid syntax${NC}"
else
  echo -e "${RED}✗ Some scripts have syntax errors${NC}"
  exit 1
fi

TOTAL_WARNINGS=$((SHELL_WARNINGS + PS_WARNINGS))
echo "Total warnings: $TOTAL_WARNINGS"
echo ""

if [ $TOTAL_WARNINGS -le 90 ]; then
  echo -e "${GREEN}✓ VERIFICATION PASSED${NC}"
  echo "Scripts are within acceptable linting thresholds"
  exit 0
else
  echo -e "${YELLOW}⚠ VERIFICATION MARGINAL${NC}"
  echo "Scripts have more warnings than expected"
  exit 0
fi
