#!/bin/bash
# Portable-Only Cleanup Script
# This script removes all MSIX/EXE packaging infrastructure to enforce portable-only distribution

set -e

DRY_RUN=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--dry-run]"
            exit 1
            ;;
    esac
done

echo "=== Aura Video Studio - Portable-Only Cleanup ==="
echo ""

if [ "$DRY_RUN" = true ]; then
    echo "DRY RUN MODE - No files will be deleted"
    echo ""
fi

# Set root directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Define patterns to search for and delete
PATTERNS=(
    "*.iss"
    "*.appx*"
    "*.msix*"
    "*.msixbundle"
    "*.cer"
)

# Define specific patterns to search in scripts/packaging/
PACKAGING_PATTERNS=(
    "*msix*"
    "*inno*"
    "*setup*"
    "*installer*"
)

echo "Step 1: Searching for artifact files..."
FILES_TO_DELETE=()

# Use associative array to track unique files
declare -A SEEN_FILES

# Search for artifact files in the entire repository
for pattern in "${PATTERNS[@]}"; do
    while IFS= read -r -d '' file; do
        # Skip node_modules and .git directories
        if [[ ! "$file" =~ /node_modules/ ]] && [[ ! "$file" =~ /\.git/ ]]; then
            # Only add if not seen before
            if [[ -z "${SEEN_FILES[$file]}" ]]; then
                FILES_TO_DELETE+=("$file")
                SEEN_FILES[$file]=1
            fi
        fi
    done < <(find "$ROOT_DIR" -name "$pattern" -type f -print0 2>/dev/null)
done

if [ ${#FILES_TO_DELETE[@]} -eq 0 ]; then
    echo "  No artifact files found"
else
    echo "  Found ${#FILES_TO_DELETE[@]} artifact file(s) to delete:"
    for file in "${FILES_TO_DELETE[@]}"; do
        relative_path="${file#$ROOT_DIR/}"
        echo "    - $relative_path"
    done
fi

echo ""
echo "Step 2: Searching for MSIX/EXE packaging scripts in scripts/packaging/..."
PACKAGING_FILES_TO_DELETE=()

PACKAGING_DIR="$ROOT_DIR/scripts/packaging"
if [ -d "$PACKAGING_DIR" ]; then
    for pattern in "${PACKAGING_PATTERNS[@]}"; do
        while IFS= read -r -d '' item; do
            # Only add if not already in the list
            if [[ -z "${SEEN_FILES[$item]}" ]]; then
                PACKAGING_FILES_TO_DELETE+=("$item")
                SEEN_FILES[$item]=1
            fi
        done < <(find "$PACKAGING_DIR" -name "$pattern" -print0 2>/dev/null)
    done
fi

if [ ${#PACKAGING_FILES_TO_DELETE[@]} -eq 0 ]; then
    echo "  No MSIX/EXE packaging files found in scripts/packaging/"
else
    echo "  Found ${#PACKAGING_FILES_TO_DELETE[@]} packaging file(s) to delete:"
    for item in "${PACKAGING_FILES_TO_DELETE[@]}"; do
        relative_path="${item#$ROOT_DIR/}"
        echo "    - $relative_path"
    done
fi

echo ""
echo "Step 3: Summary"
ALL_ITEMS_TO_DELETE=("${FILES_TO_DELETE[@]}" "${PACKAGING_FILES_TO_DELETE[@]}")
echo "  Total items to delete: ${#ALL_ITEMS_TO_DELETE[@]}"

if [ ${#ALL_ITEMS_TO_DELETE[@]} -eq 0 ]; then
    echo ""
    echo "=== Cleanup Complete - No files to delete ==="
    exit 0
fi

if [ "$DRY_RUN" = true ]; then
    echo ""
    echo "=== Dry Run Complete - No files were deleted ==="
    echo "Run without --dry-run flag to actually delete files"
    exit 0
fi

echo ""
echo "Proceeding with deletion..."

DELETED_COUNT=0
FAILED_COUNT=0

for item in "${ALL_ITEMS_TO_DELETE[@]}"; do
    relative_path="${item#$ROOT_DIR/}"
    if rm -rf "$item" 2>/dev/null; then
        echo "  ✓ Deleted: $relative_path"
        DELETED_COUNT=$((DELETED_COUNT + 1))
    else
        echo "  ✗ Failed to delete: $relative_path"
        FAILED_COUNT=$((FAILED_COUNT + 1))
    fi
done

echo ""
echo "=== Cleanup Complete ==="
echo "  Deleted: $DELETED_COUNT"
if [ $FAILED_COUNT -gt 0 ]; then
    echo "  Failed: $FAILED_COUNT"
fi
echo ""
