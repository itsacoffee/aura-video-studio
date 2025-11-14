#!/bin/bash

# Database Migration Runner for Aura Video Studio
# This script runs database migrations using Entity Framework Core

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${YELLOW}Running database migrations...${NC}"
echo ""

# Check if running in Docker or local
if [ -f "/.dockerenv" ]; then
  echo "Running in Docker container"
  cd /app
else
  echo "Running locally"
  cd "$(dirname "$0")/../.."
fi

# Check if dotnet is available
if ! command -v dotnet &>/dev/null; then
  echo -e "${RED}Error: .NET SDK not found${NC}"
  echo "Install .NET SDK or run migrations via Docker:"
  echo "  docker-compose exec api dotnet ef database update"
  exit 1
fi

# Run EF Core migrations
echo -e "${YELLOW}Applying Entity Framework migrations...${NC}"
cd Aura.Api
dotnet ef database update --verbose

if [ $? -eq 0 ]; then
  echo -e "${GREEN}✓ Migrations applied successfully${NC}"
  echo ""

  # Optionally run seed scripts
  if [ "$1" = "--seed" ]; then
    echo -e "${YELLOW}Running seed scripts...${NC}"
    ./scripts/setup/seed-database.sh
  fi
else
  echo -e "${RED}✗ Migration failed${NC}"
  exit 1
fi

echo -e "${GREEN}Database migration complete!${NC}"
