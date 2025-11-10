#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Aura Video Studio - Desktop Build${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Function to print colored messages
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    print_error "Node.js is not installed. Please install Node.js 18+ from https://nodejs.org/"
    exit 1
fi

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    print_error ".NET 8.0 SDK is not installed. Please install from https://dotnet.microsoft.com/download"
    exit 1
fi

# Parse arguments
BUILD_TARGET="all"
SKIP_FRONTEND=false
SKIP_BACKEND=false
SKIP_INSTALLER=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --target)
            BUILD_TARGET="$2"
            shift 2
            ;;
        --skip-frontend)
            SKIP_FRONTEND=true
            shift
            ;;
        --skip-backend)
            SKIP_BACKEND=true
            shift
            ;;
        --skip-installer)
            SKIP_INSTALLER=true
            shift
            ;;
        --help)
            echo "Usage: ./build-desktop.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --target <platform>    Build for specific platform (win|mac|linux|all)"
            echo "  --skip-frontend        Skip frontend build"
            echo "  --skip-backend         Skip backend build"
            echo "  --skip-installer       Skip installer creation (build directory only)"
            echo "  --help                 Show this help message"
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

print_info "Build target: $BUILD_TARGET"
echo ""

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$SCRIPT_DIR"

# ========================================
# Step 1: Build Frontend
# ========================================
if [ "$SKIP_FRONTEND" = false ]; then
    print_info "Building React frontend..."
    cd "$PROJECT_ROOT/Aura.Web"
    
    if [ ! -d "node_modules" ]; then
        print_info "Installing frontend dependencies..."
        npm install
    fi
    
    print_info "Running frontend build..."
    npm run build
    
    if [ ! -f "dist/index.html" ]; then
        print_error "Frontend build failed - dist/index.html not found"
        exit 1
    fi
    
    print_success "Frontend build complete"
    echo ""
else
    print_warning "Skipping frontend build"
    echo ""
fi

# ========================================
# Step 2: Build Backend
# ========================================
if [ "$SKIP_BACKEND" = false ]; then
    print_info "Building .NET backend for all platforms..."
    cd "$PROJECT_ROOT/Aura.Api"
    
    # Create backend output directory
    mkdir -p "$SCRIPT_DIR/backend"
    
    # Detect current platform for optimized build
    case "$(uname -s)" in
        Darwin*)
            CURRENT_PLATFORM="mac"
            ;;
        Linux*)
            CURRENT_PLATFORM="linux"
            ;;
        MINGW*|MSYS*|CYGWIN*)
            CURRENT_PLATFORM="win"
            ;;
        *)
            CURRENT_PLATFORM="linux"
            ;;
    esac
    
    # Build for current platform or all platforms
    if [ "$BUILD_TARGET" = "all" ] || [ "$BUILD_TARGET" = "win" ]; then
        print_info "Building backend for Windows (x64)..."
        dotnet publish -c Release -r win-x64 --self-contained true \
            -p:PublishSingleFile=false \
            -p:PublishTrimmed=false \
            -p:IncludeNativeLibrariesForSelfExtract=true \
            -o "$SCRIPT_DIR/backend/win-x64"
        print_success "Windows backend build complete"
    fi
    
    if [ "$BUILD_TARGET" = "all" ] || [ "$BUILD_TARGET" = "mac" ]; then
        print_info "Building backend for macOS (x64)..."
        dotnet publish -c Release -r osx-x64 --self-contained true \
            -p:PublishSingleFile=false \
            -p:PublishTrimmed=false \
            -o "$SCRIPT_DIR/backend/osx-x64"
        
        print_info "Building backend for macOS (arm64)..."
        dotnet publish -c Release -r osx-arm64 --self-contained true \
            -p:PublishSingleFile=false \
            -p:PublishTrimmed=false \
            -o "$SCRIPT_DIR/backend/osx-arm64"
        print_success "macOS backend builds complete"
    fi
    
    if [ "$BUILD_TARGET" = "all" ] || [ "$BUILD_TARGET" = "linux" ]; then
        print_info "Building backend for Linux (x64)..."
        dotnet publish -c Release -r linux-x64 --self-contained true \
            -p:PublishSingleFile=false \
            -p:PublishTrimmed=false \
            -o "$SCRIPT_DIR/backend/linux-x64"
        print_success "Linux backend build complete"
    fi
    
    print_success "Backend builds complete"
    echo ""
else
    print_warning "Skipping backend build"
    echo ""
fi

# ========================================
# Step 3: Install Electron Dependencies
# ========================================
print_info "Installing Electron dependencies..."
cd "$SCRIPT_DIR"

if [ ! -d "node_modules" ]; then
    npm install
else
    print_info "Dependencies already installed"
fi

print_success "Electron dependencies ready"
echo ""

# ========================================
# Step 4: Build Electron Installers
# ========================================
if [ "$SKIP_INSTALLER" = false ]; then
    print_info "Building Electron installers..."
    
    case "$BUILD_TARGET" in
        win)
            print_info "Building Windows installer..."
            npm run build:win
            ;;
        mac)
            print_info "Building macOS installer..."
            npm run build:mac
            ;;
        linux)
            print_info "Building Linux packages..."
            npm run build:linux
            ;;
        all)
            print_info "Building installers for all platforms..."
            npm run build:all
            ;;
        *)
            print_error "Unknown target: $BUILD_TARGET"
            exit 1
            ;;
    esac
    
    print_success "Installer build complete"
else
    print_warning "Skipping installer creation (building directory only)"
    npm run build:dir
fi

echo ""
print_success "========================================${NC}"
print_success "Build Complete!"
print_success "========================================${NC}"
echo ""
print_info "Output directory: $SCRIPT_DIR/dist"
echo ""

# List generated files
if [ -d "$SCRIPT_DIR/dist" ]; then
    print_info "Generated files:"
    ls -lh "$SCRIPT_DIR/dist" | tail -n +2 | awk '{print "  " $9 " (" $5 ")"}'
    echo ""
fi

print_info "To run the app in development mode:"
echo "  cd Aura.Desktop"
echo "  npm start"
echo ""

print_success "All done! 🎉"
