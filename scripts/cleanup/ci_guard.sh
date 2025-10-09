#!/bin/bash
# CI Guard - Prevent MSIX/EXE Packaging Patterns
# This script checks for forbidden MSIX/EXE packaging patterns and fails if found

set -e

echo "=== CI Guard: Portable-Only Policy Check ==="
echo ""

ROOT_DIR="$(git rev-parse --show-toplevel)"
VIOLATIONS_FOUND=false

# Define forbidden patterns
FORBIDDEN_FILE_PATTERNS=(
    "*.iss"
    "*.appx*"
    "*.msix*"
    "*.msixbundle"
)

FORBIDDEN_DIRECTORY_PATTERNS=(
    "*msix*"
    "*inno*"
    "*installer*"
)

echo "Checking for forbidden file patterns..."
FILES_FOUND=()

for pattern in "${FORBIDDEN_FILE_PATTERNS[@]}"; do
    while IFS= read -r -d '' file; do
        # Skip node_modules and .git directories
        if [[ ! "$file" =~ /node_modules/ ]] && [[ ! "$file" =~ /\.git/ ]]; then
            relative_path="${file#$ROOT_DIR/}"
            FILES_FOUND+=("$relative_path")
        fi
    done < <(find "$ROOT_DIR" -name "$pattern" -type f -print0 2>/dev/null)
done

if [ ${#FILES_FOUND[@]} -eq 0 ]; then
    echo "  ✓ No forbidden files found"
else
    echo "  ✗ VIOLATION: Found ${#FILES_FOUND[@]} forbidden file(s):"
    for file in "${FILES_FOUND[@]}"; do
        echo "    - $file"
    done
    VIOLATIONS_FOUND=true
fi

echo ""
echo "Checking for forbidden directory patterns in scripts/packaging/..."
DIRS_FOUND=()

PACKAGING_DIR="$ROOT_DIR/scripts/packaging"
if [ -d "$PACKAGING_DIR" ]; then
    for pattern in "${FORBIDDEN_DIRECTORY_PATTERNS[@]}"; do
        while IFS= read -r -d '' dir; do
            relative_path="${dir#$ROOT_DIR/}"
            DIRS_FOUND+=("$relative_path")
        done < <(find "$PACKAGING_DIR" -name "$pattern" -type d -print0 2>/dev/null)
    done
fi

if [ ${#DIRS_FOUND[@]} -eq 0 ]; then
    echo "  ✓ No forbidden directories found"
else
    echo "  ✗ VIOLATION: Found ${#DIRS_FOUND[@]} forbidden director(ies):"
    for dir in "${DIRS_FOUND[@]}"; do
        echo "    - $dir"
    done
    VIOLATIONS_FOUND=true
fi

echo ""
echo "Checking for MSIX/EXE references in GitHub workflows..."
WORKFLOW_VIOLATIONS=()

WORKFLOWS_DIR="$ROOT_DIR/.github/workflows"
if [ -d "$WORKFLOWS_DIR" ]; then
    while IFS= read -r file; do
        relative_path="${file#$ROOT_DIR/}"
        # Search for MSIX/EXE related terms
        if grep -qi '\(msix\|appx\|inno.*setup\|\.iss\|msbuild.*appx\|UapAppx\)' "$file"; then
            WORKFLOW_VIOLATIONS+=("$relative_path")
        fi
    done < <(find "$WORKFLOWS_DIR" -name "*.yml" -o -name "*.yaml")
fi

if [ ${#WORKFLOW_VIOLATIONS[@]} -eq 0 ]; then
    echo "  ✓ No MSIX/EXE references found in workflows"
else
    echo "  ✗ VIOLATION: Found MSIX/EXE references in ${#WORKFLOW_VIOLATIONS[@]} workflow(s):"
    for workflow in "${WORKFLOW_VIOLATIONS[@]}"; do
        echo "    - $workflow"
        # Show the matching lines
        grep -ni '\(msix\|appx\|inno.*setup\|\.iss\|msbuild.*appx\|UapAppx\)' "$ROOT_DIR/$workflow" | head -3 | while read line; do
            echo "      $line"
        done
    done
    VIOLATIONS_FOUND=true
fi

echo ""
if [ "$VIOLATIONS_FOUND" = true ]; then
    echo "=== CI GUARD FAILED ==="
    echo ""
    echo "POLICY VIOLATION: This repository enforces portable-only distribution."
    echo ""
    echo "The following are forbidden:"
    echo "  - MSIX/APPX packages and manifests"
    echo "  - Inno Setup (.iss) scripts"
    echo "  - EXE installers"
    echo "  - Related build infrastructure"
    echo ""
    echo "Please remove the violating files and references."
    echo "Run scripts/cleanup/portable_only_cleanup.sh to clean up automatically."
    exit 1
else
    echo "=== CI GUARD PASSED ==="
    echo ""
    echo "✓ Portable-only policy is enforced"
    echo "✓ No forbidden MSIX/EXE packaging found"
    exit 0
fi
