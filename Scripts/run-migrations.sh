#!/bin/bash

# Database Migration Runner Script
# This script runs EF Core migrations for the Aura database

set -e

echo "========================================="
echo "Aura Database Migration Runner"
echo "========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: .NET SDK not found. Please install .NET 8.0 SDK${NC}"
    echo "Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo "Project Root: $PROJECT_ROOT"
echo ""

# Parse command line arguments
COMMAND="${1:-migrate}"
MIGRATION_NAME="${2:-}"

case "$COMMAND" in
    migrate)
        echo -e "${YELLOW}Running database migrations...${NC}"
        cd "$PROJECT_ROOT"
        dotnet ef database update \
            --project Aura.Api \
            --startup-project Aura.Api \
            --context AuraDbContext \
            --verbose
        echo -e "${GREEN}✓ Migrations applied successfully${NC}"
        ;;
    
    add)
        if [ -z "$MIGRATION_NAME" ]; then
            echo -e "${RED}Error: Migration name is required${NC}"
            echo "Usage: ./run-migrations.sh add <MigrationName>"
            exit 1
        fi
        echo -e "${YELLOW}Creating new migration: $MIGRATION_NAME${NC}"
        cd "$PROJECT_ROOT"
        dotnet ef migrations add "$MIGRATION_NAME" \
            --project Aura.Api \
            --startup-project Aura.Api \
            --context AuraDbContext \
            --output-dir Data/Migrations
        echo -e "${GREEN}✓ Migration created successfully${NC}"
        ;;
    
    remove)
        echo -e "${YELLOW}Removing last migration...${NC}"
        cd "$PROJECT_ROOT"
        dotnet ef migrations remove \
            --project Aura.Api \
            --startup-project Aura.Api \
            --context AuraDbContext \
            --force
        echo -e "${GREEN}✓ Last migration removed${NC}"
        ;;
    
    rollback)
        TARGET_MIGRATION="${2:-0}"
        echo -e "${YELLOW}Rolling back to migration: $TARGET_MIGRATION${NC}"
        cd "$PROJECT_ROOT"
        dotnet ef database update "$TARGET_MIGRATION" \
            --project Aura.Api \
            --startup-project Aura.Api \
            --context AuraDbContext \
            --verbose
        echo -e "${GREEN}✓ Database rolled back successfully${NC}"
        ;;
    
    list)
        echo -e "${YELLOW}Listing migrations...${NC}"
        cd "$PROJECT_ROOT"
        dotnet ef migrations list \
            --project Aura.Api \
            --startup-project Aura.Api \
            --context AuraDbContext
        ;;
    
    script)
        OUTPUT_FILE="${2:-migration.sql}"
        echo -e "${YELLOW}Generating SQL script: $OUTPUT_FILE${NC}"
        cd "$PROJECT_ROOT"
        dotnet ef migrations script \
            --project Aura.Api \
            --startup-project Aura.Api \
            --context AuraDbContext \
            --output "$OUTPUT_FILE" \
            --idempotent
        echo -e "${GREEN}✓ SQL script generated: $OUTPUT_FILE${NC}"
        ;;
    
    status)
        echo -e "${YELLOW}Checking migration status...${NC}"
        cd "$PROJECT_ROOT"
        dotnet ef migrations list \
            --project Aura.Api \
            --startup-project Aura.Api \
            --context AuraDbContext \
            --no-build
        ;;
    
    help|--help|-h)
        echo "Database Migration Runner"
        echo ""
        echo "Usage: ./run-migrations.sh <command> [arguments]"
        echo ""
        echo "Commands:"
        echo "  migrate              Apply all pending migrations (default)"
        echo "  add <name>          Create a new migration"
        echo "  remove              Remove the last migration"
        echo "  rollback [target]   Rollback to a specific migration (or '0' for all)"
        echo "  list                List all migrations"
        echo "  script [file]       Generate SQL script for migrations"
        echo "  status              Show migration status"
        echo "  help                Show this help message"
        echo ""
        echo "Examples:"
        echo "  ./run-migrations.sh migrate"
        echo "  ./run-migrations.sh add AddUserTable"
        echo "  ./run-migrations.sh rollback InitialCreate"
        echo "  ./run-migrations.sh script migration.sql"
        ;;
    
    *)
        echo -e "${RED}Error: Unknown command: $COMMAND${NC}"
        echo "Run './run-migrations.sh help' for usage information"
        exit 1
        ;;
esac

echo ""
echo -e "${GREEN}Done!${NC}"
