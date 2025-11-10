#!/bin/bash

# Database Restore Script
# Restores the database from a backup file

set -e

echo "========================================="
echo "Aura Database Restore Utility"
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

# Parse arguments
BACKUP_FILE="$1"
DB_PATH="${2:-$PROJECT_ROOT/aura.db}"

# Check if backup file is provided
if [ -z "$BACKUP_FILE" ]; then
    echo -e "${RED}Error: Backup file not specified${NC}"
    echo ""
    echo "Usage: ./restore-database.sh <backup-file> [database-path]"
    echo ""
    echo "Available backups:"
    ls -1t "$PROJECT_ROOT/backups"/aura_backup_*.db 2>/dev/null || echo "  (none found)"
    exit 1
fi

# Check if backup file exists
if [ ! -f "$BACKUP_FILE" ]; then
    echo -e "${RED}Error: Backup file not found: $BACKUP_FILE${NC}"
    exit 1
fi

echo "Backup file: $BACKUP_FILE"
echo "Target database: $DB_PATH"
echo ""

# Create backup of current database before restoring
if [ -f "$DB_PATH" ]; then
    echo -e "${YELLOW}Creating backup of current database...${NC}"
    TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
    SAFETY_BACKUP="$PROJECT_ROOT/backups/aura_pre_restore_$TIMESTAMP.db"
    mkdir -p "$PROJECT_ROOT/backups"
    cp "$DB_PATH" "$SAFETY_BACKUP"
    echo -e "${GREEN}✓ Current database backed up to: $SAFETY_BACKUP${NC}"
    echo ""
fi

# Confirm restore
read -p "Are you sure you want to restore from this backup? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Restore cancelled"
    exit 0
fi

# Restore database
echo -e "${YELLOW}Restoring database...${NC}"
cp "$BACKUP_FILE" "$DB_PATH"

# Verify restore
if [ -f "$DB_PATH" ]; then
    echo -e "${GREEN}✓ Database restored successfully${NC}"
    
    # Show file size
    DB_SIZE=$(stat -f%z "$DB_PATH" 2>/dev/null || stat -c%s "$DB_PATH" 2>/dev/null)
    echo "Size: $(numfmt --to=iec-i --suffix=B $DB_SIZE 2>/dev/null || echo "$DB_SIZE bytes")"
else
    echo -e "${RED}Error: Database restore failed${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}Done!${NC}"
echo ""
echo "If you need to undo this restore, the previous database was saved to:"
echo "  $SAFETY_BACKUP"
