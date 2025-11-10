#!/bin/bash

# Database Backup Script
# Creates a timestamped backup of the SQLite database

set -e

echo "========================================="
echo "Aura Database Backup Utility"
echo "========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Default database location (update based on your configuration)
DB_PATH="${1:-$PROJECT_ROOT/aura.db}"
BACKUP_DIR="${2:-$PROJECT_ROOT/backups}"

# Create backup directory if it doesn't exist
mkdir -p "$BACKUP_DIR"

# Check if database exists
if [ ! -f "$DB_PATH" ]; then
    echo -e "${RED}Error: Database not found at: $DB_PATH${NC}"
    exit 1
fi

# Generate timestamp
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="$BACKUP_DIR/aura_backup_$TIMESTAMP.db"

echo "Database: $DB_PATH"
echo "Backup to: $BACKUP_FILE"
echo ""

# Create backup
echo -e "${YELLOW}Creating backup...${NC}"
cp "$DB_PATH" "$BACKUP_FILE"

# Verify backup
if [ -f "$BACKUP_FILE" ]; then
    ORIGINAL_SIZE=$(stat -f%z "$DB_PATH" 2>/dev/null || stat -c%s "$DB_PATH" 2>/dev/null)
    BACKUP_SIZE=$(stat -f%z "$BACKUP_FILE" 2>/dev/null || stat -c%s "$BACKUP_FILE" 2>/dev/null)
    
    if [ "$ORIGINAL_SIZE" -eq "$BACKUP_SIZE" ]; then
        echo -e "${GREEN}✓ Backup created successfully${NC}"
        echo "Size: $(numfmt --to=iec-i --suffix=B $BACKUP_SIZE 2>/dev/null || echo "$BACKUP_SIZE bytes")"
    else
        echo -e "${RED}Error: Backup size mismatch${NC}"
        exit 1
    fi
else
    echo -e "${RED}Error: Backup file not created${NC}"
    exit 1
fi

# Clean up old backups (keep last 10)
echo ""
echo -e "${YELLOW}Cleaning up old backups...${NC}"
BACKUP_COUNT=$(ls -1 "$BACKUP_DIR"/aura_backup_*.db 2>/dev/null | wc -l)

if [ "$BACKUP_COUNT" -gt 10 ]; then
    REMOVE_COUNT=$((BACKUP_COUNT - 10))
    ls -1t "$BACKUP_DIR"/aura_backup_*.db | tail -n "$REMOVE_COUNT" | xargs rm -f
    echo -e "${GREEN}✓ Removed $REMOVE_COUNT old backup(s)${NC}"
else
    echo "Keeping all $BACKUP_COUNT backup(s)"
fi

echo ""
echo -e "${GREEN}Done!${NC}"
echo ""
echo "To restore this backup, run:"
echo "  cp $BACKUP_FILE $DB_PATH"
