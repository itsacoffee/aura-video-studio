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
if ! command -v node &>/dev/null; then
  print_error "Node.js is not installed. Please install Node.js 18+ from https://nodejs.org/"
  exit 1
fi

# Check if dotnet is installed
if ! command -v dotnet &>/dev/null; then
  print_error ".NET 8.0 SDK is not installed. Please install from https://dotnet.microsoft.com/download"
  exit 1
fi

# Parse arguments
BUILD_TARGET="win"
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
      echo "  --target <platform>    Build for specific platform (win only, default: win)"
      echo "  --skip-frontend        Skip frontend build"
      echo "  --skip-backend         Skip backend build"
      echo "  --skip-installer       Skip installer creation (build directory only)"
      echo "  --help                 Show this help message"
      echo ""
      echo "Note: Only Windows builds are currently supported."
      echo "      macOS and Linux builds are disabled."
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
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$SCRIPT_DIR"

# ========================================
# Step 1: Build Frontend
# ========================================
if [ "$SKIP_FRONTEND" = false ]; then
  print_info "Building React frontend..."
  cd "$PROJECT_ROOT/Aura.Web"

  # Always check and install dependencies to ensure they're up to date
  if [ ! -d "node_modules" ]; then
    print_info "Installing frontend dependencies..."
    npm install || {
      print_error "Failed to install frontend dependencies"
      exit 1
    }
  else
    # Verify critical dependencies exist
    MISSING_PACKAGES=()
    for package in vite react typescript; do
      if [ ! -d "node_modules/$package" ]; then
        MISSING_PACKAGES+=("$package")
      fi
    done
    
    if [ ${#MISSING_PACKAGES[@]} -gt 0 ]; then
      print_info "Critical dependencies missing, reinstalling..."
      print_info "Missing: ${MISSING_PACKAGES[*]}"
      npm install || {
        print_error "Failed to install frontend dependencies"
        exit 1
      }
    else
      print_info "Frontend dependencies verified"
    fi
  fi

  print_info "Running frontend build..."
  npm run build || {
    print_error "Frontend build failed"
    exit 1
  }

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
  print_info "Building .NET backend for Windows..."
  cd "$PROJECT_ROOT/Aura.Api"

  # Create backend output directory
  mkdir -p "$SCRIPT_DIR/resources/backend"

  # Only build for Windows (target platform)
  if [ "$BUILD_TARGET" = "win" ]; then
    print_info "Building backend for Windows (x64)..."
    dotnet publish -c Release -r win-x64 --self-contained true \
      -p:PublishSingleFile=false \
      -p:PublishTrimmed=false \
      -p:IncludeNativeLibrariesForSelfExtract=true \
      -p:SkipFrontendBuild=true \
      -o "$SCRIPT_DIR/resources/backend/win-x64" || {
      print_error "Windows backend build failed"
      exit 1
    }
    print_success "Windows backend build complete"
  else
    print_error "Unsupported build target: $BUILD_TARGET"
    print_error "Only Windows (win) is supported."
    exit 1
  fi

  print_success "Backend build complete"
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
  print_info "Installing Electron dependencies (node_modules not found)..."
  npm install || {
    print_error "Failed to install Electron dependencies"
    exit 1
  }
else
  # Verify critical dependencies exist
  MISSING_PACKAGES=()
  for package in electron electron-builder electron-store; do
    if [ ! -d "node_modules/$package" ]; then
      MISSING_PACKAGES+=("$package")
    fi
  done
  
  if [ ${#MISSING_PACKAGES[@]} -gt 0 ]; then
    print_info "Critical Electron dependencies missing, reinstalling..."
    print_info "Missing: ${MISSING_PACKAGES[*]}"
    npm install || {
      print_error "Failed to install Electron dependencies"
      exit 1
    }
  else
    print_info "Electron dependencies verified"
  fi
fi

print_success "Electron dependencies ready"
echo ""

# ========================================
# Step 4: Validate Resources
# ========================================
print_info "Validating required resources..."

VALIDATION_FAILED=false

if [ ! -f "$PROJECT_ROOT/Aura.Web/dist/index.html" ]; then
  print_error "Frontend build not found at: $PROJECT_ROOT/Aura.Web/dist/index.html"
  VALIDATION_FAILED=true
else
  print_success "  ✓ Frontend build found"
fi

if [ ! -d "$SCRIPT_DIR/resources/backend" ]; then
  print_error "Backend binaries not found at: $SCRIPT_DIR/resources/backend"
  VALIDATION_FAILED=true
else
  print_success "  ✓ Backend binaries found"
fi

if [ "$VALIDATION_FAILED" = true ]; then
  print_error "Resource validation failed. Cannot build installer."
  print_info "Please ensure all build steps complete successfully."
  exit 1
fi

print_success "All required resources validated"
echo ""

# ========================================
# Step 5: Build Electron Installers
# ========================================
if [ "$SKIP_INSTALLER" = false ]; then
  print_info "Building Electron installers..."

  case "$BUILD_TARGET" in
    win)
      print_info "Building Windows installer..."
      npm run build:win || {
        print_error "Windows installer build failed"
        exit 1
      }
      ;;
    mac | linux | all)
      print_error "Only Windows builds are currently supported."
      print_error "macOS and Linux builds have been disabled."
      print_info "Use --target win to build for Windows."
      exit 1
      ;;
    *)
      print_error "Unknown target: $BUILD_TARGET"
      print_info "Use --help for usage information."
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
