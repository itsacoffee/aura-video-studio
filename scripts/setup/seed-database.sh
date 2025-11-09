#!/bin/bash

# Database Seed Script for Aura Video Studio
# This script populates the database with test data for local development

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${YELLOW}Seeding database with test data...${NC}"
echo ""

# Get database path
DB_PATH="${AURA_DATABASE_PATH:-data/aura.db}"

# Check if database exists
if [ ! -f "$DB_PATH" ]; then
    echo -e "${RED}Error: Database not found at $DB_PATH${NC}"
    echo "Run migrations first: ./scripts/setup/migrate.sh"
    exit 1
fi

# Check if sqlite3 is available
if ! command -v sqlite3 &> /dev/null; then
    echo -e "${YELLOW}Warning: sqlite3 not found, using .NET to seed${NC}"
    echo "Seed data will be created when the API starts (via SeedData.cs)"
    exit 0
fi

# Run seed scripts in order
echo -e "${YELLOW}Running seed: 001_test_users.sql${NC}"
sqlite3 "$DB_PATH" < seeds/001_test_users.sql

echo -e "${YELLOW}Running seed: 002_sample_projects.sql${NC}"
sqlite3 "$DB_PATH" < seeds/002_sample_projects.sql

echo -e "${GREEN}✓ Database seeded successfully${NC}"
echo ""
echo "Sample data created:"
echo "  - User setup (wizard completed)"
echo "  - 3 sample projects"
echo "  - 1 in-progress wizard project"
echo "  - Sample action logs"
echo ""
