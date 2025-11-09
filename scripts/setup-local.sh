#!/bin/bash

# Aura Video Studio - Local Development Setup Script (Linux/macOS)
# This script bootstraps the complete local development environment

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
MIN_DOCKER_VERSION="20.0.0"
MIN_NODE_VERSION="20.0.0"
MIN_DOTNET_VERSION="8.0.0"

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Aura Video Studio - Local Development Setup          ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}"
echo ""

# Function to compare versions
version_ge() {
    [ "$(printf '%s\n' "$1" "$2" | sort -V | head -n 1)" = "$2" ]
}

# Function to check command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo -e "${YELLOW}[1/7] Checking prerequisites...${NC}"

# Check Docker
if ! command_exists docker; then
    echo -e "${RED}✗ Docker is not installed${NC}"
    echo -e "  Please install Docker Desktop from: https://www.docker.com/products/docker-desktop"
    exit 1
fi

DOCKER_VERSION=$(docker --version | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | head -n 1)
if ! version_ge "$DOCKER_VERSION" "$MIN_DOCKER_VERSION"; then
    echo -e "${RED}✗ Docker version $DOCKER_VERSION is too old (need >= $MIN_DOCKER_VERSION)${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Docker $DOCKER_VERSION${NC}"

# Check Docker Compose
if ! command_exists docker-compose && ! docker compose version >/dev/null 2>&1; then
    echo -e "${RED}✗ Docker Compose is not installed${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Docker Compose${NC}"

# Check .NET SDK
if ! command_exists dotnet; then
    echo -e "${YELLOW}⚠ .NET SDK is not installed${NC}"
    echo -e "  Recommended for local development: https://dotnet.microsoft.com/download"
    echo -e "  (Not required if using Docker only)"
else
    DOTNET_VERSION=$(dotnet --version | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')
    if version_ge "$DOTNET_VERSION" "$MIN_DOTNET_VERSION"; then
        echo -e "${GREEN}✓ .NET SDK $DOTNET_VERSION${NC}"
    else
        echo -e "${YELLOW}⚠ .NET SDK $DOTNET_VERSION is old (recommend >= $MIN_DOTNET_VERSION)${NC}"
    fi
fi

# Check Node.js
if ! command_exists node; then
    echo -e "${YELLOW}⚠ Node.js is not installed${NC}"
    echo -e "  Recommended for local development: https://nodejs.org/"
    echo -e "  (Not required if using Docker only)"
else
    NODE_VERSION=$(node --version | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')
    if version_ge "$NODE_VERSION" "$MIN_NODE_VERSION"; then
        echo -e "${GREEN}✓ Node.js $NODE_VERSION${NC}"
    else
        echo -e "${YELLOW}⚠ Node.js $NODE_VERSION is old (recommend >= $MIN_NODE_VERSION)${NC}"
    fi
fi

# Check for FFmpeg (optional but recommended)
if command_exists ffmpeg; then
    FFMPEG_VERSION=$(ffmpeg -version 2>/dev/null | head -n 1 | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | head -n 1)
    echo -e "${GREEN}✓ FFmpeg $FFMPEG_VERSION (local)${NC}"
else
    echo -e "${YELLOW}⚠ FFmpeg not installed locally (will use Docker container)${NC}"
fi

echo ""

# Create required directories
echo -e "${YELLOW}[2/7] Creating directory structure...${NC}"
mkdir -p data logs temp-media scripts/setup
echo -e "${GREEN}✓ Directories created${NC}"
echo ""

# Setup environment file
echo -e "${YELLOW}[3/7] Setting up environment configuration...${NC}"
if [ ! -f .env ]; then
    cp .env.example .env
    echo -e "${GREEN}✓ Created .env from .env.example${NC}"
    echo -e "${BLUE}  Edit .env to add your API keys (optional)${NC}"
else
    echo -e "${BLUE}  .env already exists, skipping${NC}"
fi
echo ""

# Check port availability
echo -e "${YELLOW}[4/7] Checking port availability...${NC}"
PORTS_IN_USE=()

check_port() {
    local port=$1
    local service=$2
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1 || nc -z localhost $port 2>/dev/null; then
        PORTS_IN_USE+=("$port ($service)")
        echo -e "${RED}✗ Port $port ($service) is already in use${NC}"
        return 1
    else
        echo -e "${GREEN}✓ Port $port ($service) is available${NC}"
        return 0
    fi
}

check_port 5005 "API"
check_port 3000 "Web"
check_port 6379 "Redis"

if [ ${#PORTS_IN_USE[@]} -gt 0 ]; then
    echo -e "${YELLOW}Warning: Some ports are in use. Services may fail to start.${NC}"
    echo -e "${YELLOW}Consider stopping conflicting services or changing ports in docker-compose.yml${NC}"
fi
echo ""

# Pull Docker images
echo -e "${YELLOW}[5/7] Pulling Docker images...${NC}"
docker-compose pull redis ffmpeg 2>/dev/null || true
echo -e "${GREEN}✓ Base images ready${NC}"
echo ""

# Install dependencies (if running locally)
echo -e "${YELLOW}[6/7] Installing dependencies...${NC}"
if command_exists dotnet && [ -f "Aura.Api/Aura.Api.csproj" ]; then
    echo -e "${BLUE}  Installing .NET packages...${NC}"
    dotnet restore
fi

if command_exists npm && [ -f "Aura.Web/package.json" ]; then
    echo -e "${BLUE}  Installing Node.js packages...${NC}"
    cd Aura.Web && npm ci && cd ..
fi
echo -e "${GREEN}✓ Dependencies installed${NC}"
echo ""

# Create helper scripts
echo -e "${YELLOW}[7/7] Creating helper scripts...${NC}"

# Port check script
cat > scripts/setup/check-ports.sh << 'EOF'
#!/bin/bash
PORTS=(5005 3000 6379)
SERVICES=("API" "Web" "Redis")
CONFLICT=0

for i in "${!PORTS[@]}"; do
    if lsof -Pi :${PORTS[$i]} -sTCP:LISTEN -t >/dev/null 2>&1 || nc -z localhost ${PORTS[$i]} 2>/dev/null; then
        echo "⚠ Port ${PORTS[$i]} (${SERVICES[$i]}) is in use"
        CONFLICT=1
    fi
done

exit $CONFLICT
EOF
chmod +x scripts/setup/check-ports.sh

# Validation script
cat > scripts/setup/validate-config.sh << 'EOF'
#!/bin/bash
echo "Validating configuration files..."

# Check required files
FILES=("docker-compose.yml" "Makefile" ".env")
for file in "${FILES[@]}"; do
    if [ ! -f "$file" ]; then
        echo "✗ Missing required file: $file"
        exit 1
    fi
done

# Check .env has required variables
REQUIRED_VARS=("ASPNETCORE_ENVIRONMENT" "AURA_DATABASE_PATH")
for var in "${REQUIRED_VARS[@]}"; do
    if ! grep -q "^$var=" .env 2>/dev/null && ! grep -q "^# *$var=" .env 2>/dev/null; then
        echo "⚠ Missing variable in .env: $var"
    fi
done

echo "✓ Configuration valid"
EOF
chmod +x scripts/setup/validate-config.sh

echo -e "${GREEN}✓ Helper scripts created${NC}"
echo ""

# Summary
echo -e "${GREEN}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  Setup Complete!                                       ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo ""
echo -e "  1. ${YELLOW}(Optional)${NC} Edit ${BLUE}.env${NC} to add API keys for premium features"
echo ""
echo -e "  2. Start the development environment:"
echo -e "     ${GREEN}make dev${NC}"
echo ""
echo -e "  3. Wait ~60 seconds for services to start, then open:"
echo -e "     ${BLUE}http://localhost:3000${NC}"
echo ""
echo -e "  4. View logs in another terminal:"
echo -e "     ${GREEN}make logs${NC}"
echo ""
echo -e "  5. Check service health:"
echo -e "     ${GREEN}make health${NC}"
echo ""
echo -e "${BLUE}Useful Commands:${NC}"
echo -e "  ${GREEN}make help${NC}        - Show all available commands"
echo -e "  ${GREEN}make stop${NC}        - Stop all services"
echo -e "  ${GREEN}make clean${NC}       - Remove all containers and data"
echo -e "  ${GREEN}make db-reset${NC}    - Reset the database"
echo ""
echo -e "${YELLOW}Troubleshooting:${NC}"
echo -e "  If services fail to start, check:"
echo -e "  • Port conflicts: ${GREEN}make status${NC}"
echo -e "  • Docker is running: ${GREEN}docker ps${NC}"
echo -e "  • Logs for errors: ${GREEN}make logs${NC}"
echo ""
echo -e "  See ${BLUE}DEVELOPMENT.md${NC} and ${BLUE}docs/troubleshooting/${NC} for more help"
echo ""

exit 0
