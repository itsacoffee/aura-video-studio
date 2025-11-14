#!/usr/bin/env bash
#
# Quality Gates Checker
# Runs all quality gates locally before committing/pushing
#
# Usage:
#   ./scripts/check-quality-gates.sh [options]
#
# Options:
#   --frontend-only    Check only frontend quality gates
#   --backend-only     Check only backend quality gates
#   --docs-only        Check only documentation quality gates
#   --scripts-only     Check only scripts quality gates
#   --strict           Exit on first failure (default: report all)
#   --help             Show this help message

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Counters
PASSED=0
FAILED=0
WARNINGS=0

# Options
CHECK_FRONTEND=true
CHECK_BACKEND=true
CHECK_DOCS=true
CHECK_SCRIPTS=true
STRICT_MODE=false

# Parse command line arguments
for arg in "$@"; do
  case $arg in
    --frontend-only)
      CHECK_BACKEND=false
      CHECK_DOCS=false
      CHECK_SCRIPTS=false
      ;;
    --backend-only)
      CHECK_FRONTEND=false
      CHECK_DOCS=false
      CHECK_SCRIPTS=false
      ;;
    --docs-only)
      CHECK_FRONTEND=false
      CHECK_BACKEND=false
      CHECK_SCRIPTS=false
      ;;
    --scripts-only)
      CHECK_FRONTEND=false
      CHECK_BACKEND=false
      CHECK_DOCS=false
      ;;
    --strict)
      STRICT_MODE=true
      ;;
    --help)
      head -n 15 "$0" | tail -n 13
      exit 0
      ;;
    *)
      echo "Unknown option: $arg"
      echo "Use --help for usage information"
      exit 1
      ;;
  esac
done

# Helper functions
print_header() {
  echo ""
  echo -e "${CYAN}========================================${NC}"
  echo -e "${CYAN}$1${NC}"
  echo -e "${CYAN}========================================${NC}"
  echo ""
}

print_section() {
  echo ""
  echo -e "${BLUE}>>> $1${NC}"
  echo ""
}

check_passed() {
  echo -e "${GREEN}✓ $1${NC}"
  ((PASSED++))
}

check_failed() {
  echo -e "${RED}✗ $1${NC}"
  ((FAILED++))
  if [ "$STRICT_MODE" = true ]; then
    echo ""
    echo -e "${RED}Strict mode: Exiting on first failure${NC}"
    exit 1
  fi
}

check_warning() {
  echo -e "${YELLOW}⚠ $1${NC}"
  ((WARNINGS++))
}

# Get repository root
REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null || echo ".")
cd "$REPO_ROOT"

print_header "Quality Gates Checker"
echo "Repository: $REPO_ROOT"
echo "Mode: $([ "$STRICT_MODE" = true ] && echo "Strict (exit on first failure)" || echo "Report all")"
echo ""

# Frontend Quality Gates
if [ "$CHECK_FRONTEND" = true ]; then
  print_header "Frontend Quality Gates"
  
  if [ ! -d "Aura.Web" ]; then
    check_warning "Aura.Web directory not found, skipping frontend checks"
  else
    cd Aura.Web
    
    # Check if node_modules exists
    if [ ! -d "node_modules" ]; then
      print_section "Installing dependencies (npm ci)"
      npm ci || check_failed "npm ci failed"
    fi
    
    print_section "ESLint (zero warnings)"
    if npm run lint 2>&1 | tee /tmp/eslint.log; then
      check_passed "ESLint: No warnings or errors"
    else
      check_failed "ESLint: Warnings or errors found (see output above)"
    fi
    
    print_section "TypeScript type check"
    if npm run typecheck 2>&1 | tee /tmp/typecheck.log; then
      check_passed "TypeScript: No type errors"
    else
      check_failed "TypeScript: Type errors found (see output above)"
    fi
    
    print_section "Prettier formatting check"
    if npm run format:check 2>&1 | tee /tmp/prettier.log; then
      check_passed "Prettier: Code is formatted correctly"
    else
      check_failed "Prettier: Code formatting issues found (run 'npm run format' to fix)"
    fi
    
    print_section "Stylelint (CSS)"
    if [ -f "package.json" ] && grep -q "lint:css" package.json; then
      if npm run lint:css 2>&1 | tee /tmp/stylelint.log; then
        check_passed "Stylelint: No CSS warnings"
      else
        check_failed "Stylelint: CSS warnings found (run 'npm run lint:css:fix' to fix)"
      fi
    else
      check_warning "Stylelint: Script not found, skipping"
    fi
    
    cd "$REPO_ROOT"
  fi
fi

# Backend Quality Gates
if [ "$CHECK_BACKEND" = true ]; then
  print_header "Backend Quality Gates"
  
  if ! command -v dotnet &> /dev/null; then
    check_warning ".NET SDK not found, skipping backend checks"
  else
    print_section "Restore NuGet packages"
    if dotnet restore 2>&1 | tee /tmp/dotnet-restore.log; then
      check_passed ".NET Restore: Completed"
    else
      check_failed ".NET Restore: Failed"
    fi
    
    print_section ".NET Build (warnings as errors)"
    if dotnet build --configuration Release --no-restore 2>&1 | tee /tmp/dotnet-build.log; then
      check_passed ".NET Build: No warnings"
    else
      check_failed ".NET Build: Warnings or errors found (see output above)"
    fi
    
    print_section "dotnet format (verify no changes)"
    if dotnet format --verify-no-changes --verbosity quiet 2>&1 | tee /tmp/dotnet-format.log; then
      check_passed "dotnet format: Code is formatted correctly"
    else
      check_failed "dotnet format: Formatting changes needed (run 'dotnet format' to fix)"
    fi
  fi
fi

# Documentation Quality Gates
if [ "$CHECK_DOCS" = true ]; then
  print_header "Documentation Quality Gates"
  
  if ! command -v dotnet &> /dev/null; then
    check_warning ".NET SDK not found, skipping DocFX check"
  else
    print_section "DocFX build (warnings as errors)"
    if command -v docfx &> /dev/null || [ -f "$HOME/.dotnet/tools/docfx" ]; then
      DOCFX_CMD=$(command -v docfx || echo "$HOME/.dotnet/tools/docfx")
      if $DOCFX_CMD build docfx.json --warningsAsErrors 2>&1 | tee /tmp/docfx.log; then
        check_passed "DocFX: Documentation built without warnings"
      else
        check_failed "DocFX: Warnings found in documentation build"
      fi
    else
      check_warning "DocFX not installed (run 'dotnet tool install -g docfx')"
    fi
  fi
  
  print_section "Markdown link check"
  if command -v markdown-link-check &> /dev/null; then
    BROKEN_LINKS=0
    for mdfile in $(find docs -name "*.md" 2>/dev/null || echo ""); do
      if [ -f "$mdfile" ]; then
        markdown-link-check "$mdfile" || ((BROKEN_LINKS++))
      fi
    done
    
    if [ $BROKEN_LINKS -eq 0 ]; then
      check_passed "Link check: No broken links found"
    else
      check_failed "Link check: $BROKEN_LINKS files with broken links"
    fi
  else
    check_warning "markdown-link-check not installed (run 'npm install -g markdown-link-check')"
  fi
fi

# Scripts Quality Gates
if [ "$CHECK_SCRIPTS" = true ]; then
  print_header "Scripts Quality Gates"
  
  print_section "Shellcheck (shell scripts)"
  if command -v shellcheck &> /dev/null; then
    SHELL_ISSUES=0
    for script in $(find scripts -name "*.sh" 2>/dev/null || echo ""); do
      if [ -f "$script" ]; then
        if ! shellcheck --severity=warning "$script" 2>&1 | tee -a /tmp/shellcheck.log; then
          ((SHELL_ISSUES++))
        fi
      fi
    done
    
    if [ $SHELL_ISSUES -eq 0 ]; then
      check_passed "Shellcheck: No warnings in shell scripts"
    else
      check_failed "Shellcheck: $SHELL_ISSUES scripts with warnings"
    fi
  else
    check_warning "Shellcheck not installed (install via package manager)"
  fi
  
  print_section "PSScriptAnalyzer (PowerShell scripts)"
  if command -v pwsh &> /dev/null; then
    PS_ISSUES=$(pwsh -Command "
      Install-Module -Name PSScriptAnalyzer -Force -Scope CurrentUser -ErrorAction SilentlyContinue
      \$issues = 0
      Get-ChildItem -Path scripts -Filter *.ps1 -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        \$results = Invoke-ScriptAnalyzer -Path \$_.FullName -Severity Warning,Error -ErrorAction SilentlyContinue
        if (\$results) { \$issues += \$results.Count }
      }
      \$issues
    " 2>&1)
    
    if [ "$PS_ISSUES" = "0" ]; then
      check_passed "PSScriptAnalyzer: No warnings in PowerShell scripts"
    else
      check_failed "PSScriptAnalyzer: $PS_ISSUES issues found"
    fi
  else
    check_warning "PowerShell not installed, skipping PSScriptAnalyzer"
  fi
fi

# Summary
print_header "Quality Gates Summary"

echo ""
echo -e "Passed:   ${GREEN}$PASSED${NC}"
echo -e "Failed:   ${RED}$FAILED${NC}"
echo -e "Warnings: ${YELLOW}$WARNINGS${NC}"
echo ""

if [ $FAILED -gt 0 ]; then
  echo -e "${RED}✗ Quality gates failed${NC}"
  echo ""
  echo "Fix the issues above before committing or pushing."
  echo "See BUILD_GUIDE.md for remediation steps."
  exit 1
elif [ $WARNINGS -gt 0 ]; then
  echo -e "${YELLOW}⚠ Quality gates passed with warnings${NC}"
  echo ""
  echo "Some checks were skipped due to missing tools."
  echo "Install the tools to run all checks."
  exit 0
else
  echo -e "${GREEN}✓ All quality gates passed!${NC}"
  echo ""
  echo "Your code is ready to commit and push."
  exit 0
fi
