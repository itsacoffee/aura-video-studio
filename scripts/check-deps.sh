#!/bin/bash

# Aura Video Studio - Dependency Check Script
# This script validates all required and optional dependencies
# and provides a comprehensive system readiness report.

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Symbols
CHECK_MARK="✓"
CROSS_MARK="✗"
WARNING="⚠"

echo "================================================"
echo "  Aura Video Studio - Dependency Check"
echo "================================================"
echo ""

# Track overall status
CRITICAL_FAILURES=0
WARNINGS=0
OPTIONAL_MISSING=0

# Function to check command availability
check_command() {
  local cmd=$1
  local name=$2
  local critical=$3

  echo -n "Checking $name... "

  if command -v "$cmd" &>/dev/null; then
    echo -e "${GREEN}${CHECK_MARK} Found${NC}"

    # Get version if available
    case $cmd in
      ffmpeg)
        VERSION=$($cmd -version 2>&1 | head -n 1)
        echo "  Version: $VERSION"
        ;;
      python | python3)
        VERSION=$($cmd --version 2>&1)
        echo "  Version: $VERSION"
        ;;
      dotnet)
        VERSION=$($cmd --version 2>&1)
        echo "  Version: .NET $VERSION"
        ;;
      node)
        VERSION=$($cmd --version 2>&1)
        echo "  Version: $VERSION"
        ;;
    esac

    # Get path
    PATH_TO_CMD=$(which "$cmd")
    echo "  Path: $PATH_TO_CMD"
    echo ""
    return 0
  else
    if [ "$critical" = "true" ]; then
      echo -e "${RED}${CROSS_MARK} NOT FOUND (CRITICAL)${NC}"
      CRITICAL_FAILURES=$((CRITICAL_FAILURES + 1))
    else
      echo -e "${YELLOW}${WARNING} NOT FOUND (OPTIONAL)${NC}"
      OPTIONAL_MISSING=$((OPTIONAL_MISSING + 1))
    fi
    echo ""
    return 1
  fi
}

# Function to check Python package
check_pip_package() {
  local package=$1
  local name=$2

  echo -n "Checking Python package: $name... "

  # First check if pip is available
  if ! command -v pip &>/dev/null && ! command -v pip3 &>/dev/null; then
    echo -e "${YELLOW}${WARNING} pip not found${NC}"
    echo ""
    return 1
  fi

  # Use pip or pip3
  local PIP_CMD="pip"
  if ! command -v pip &>/dev/null; then
    PIP_CMD="pip3"
  fi

  if $PIP_CMD show "$package" &>/dev/null; then
    VERSION=$($PIP_CMD show "$package" 2>&1 | grep "Version:" | cut -d' ' -f2)
    echo -e "${GREEN}${CHECK_MARK} Installed${NC}"
    echo "  Version: $VERSION"
    echo ""
    return 0
  else
    echo -e "${YELLOW}${WARNING} NOT INSTALLED${NC}"
    echo "  Install: $PIP_CMD install $package"
    echo ""
    OPTIONAL_MISSING=$((OPTIONAL_MISSING + 1))
    return 1
  fi
}

# Function to check GPU
check_gpu() {
  echo -n "Checking GPU availability... "

  if command -v nvidia-smi &>/dev/null; then
    if nvidia-smi &>/dev/null; then
      echo -e "${GREEN}${CHECK_MARK} NVIDIA GPU detected${NC}"
      GPU_INFO=$(nvidia-smi --query-gpu=name,memory.total --format=csv,noheader 2>&1 | head -n 1)
      echo "  GPU: $GPU_INFO"

      # Check CUDA
      if command -v nvcc &>/dev/null; then
        CUDA_VERSION=$(nvcc --version 2>&1 | grep "release" | sed -n 's/.*release \([0-9.]*\).*/\1/p')
        echo "  CUDA Version: $CUDA_VERSION"
      else
        echo -e "  ${YELLOW}CUDA Toolkit not found${NC}"
      fi
      echo ""
      return 0
    fi
  fi

  echo -e "${YELLOW}${WARNING} No NVIDIA GPU detected${NC}"
  echo "  Note: Application will use CPU for AI processing (slower)"
  echo ""
  return 1
}

# Function to check disk space
check_disk_space() {
  echo "Checking disk space..."

  # Get home directory disk space
  if command -v df &>/dev/null; then
    SPACE=$(df -h ~ | awk 'NR==2 {print $4}')
    echo -e "  Available space: ${BLUE}$SPACE${NC}"

    # Convert to MB for comparison
    SPACE_MB=$(df -m ~ | awk 'NR==2 {print $4}')

    if [ "$SPACE_MB" -lt 2048 ]; then
      echo -e "  ${RED}${CROSS_MARK} Less than 2GB available (CRITICAL)${NC}"
      CRITICAL_FAILURES=$((CRITICAL_FAILURES + 1))
    elif [ "$SPACE_MB" -lt 10240 ]; then
      echo -e "  ${YELLOW}${WARNING} Less than 10GB available (recommended)${NC}"
      WARNINGS=$((WARNINGS + 1))
    else
      echo -e "  ${GREEN}${CHECK_MARK} Sufficient disk space${NC}"
    fi
  else
    echo -e "  ${YELLOW}Unable to check disk space${NC}"
  fi
  echo ""
}

# Function to check directory permissions
check_directories() {
  echo "Checking directory permissions..."

  # Determine platform-specific paths
  if [[ "$OSTYPE" == "darwin"* ]]; then
    DATA_DIR="$HOME/Library/Application Support/Aura"
    OUTPUT_DIR="$HOME/Movies/Aura"
    PROJECTS_DIR="$HOME/Documents/Aura Projects"
  elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    DATA_DIR="$LOCALAPPDATA/Aura"
    OUTPUT_DIR="$USERPROFILE/Videos/Aura"
    PROJECTS_DIR="$USERPROFILE/Documents/Aura Projects"
  else
    DATA_DIR="$HOME/.local/share/aura"
    OUTPUT_DIR="$HOME/Videos/Aura"
    PROJECTS_DIR="$HOME/Documents/Aura Projects"
  fi

  # Check/create directories
  for DIR in "$DATA_DIR" "$OUTPUT_DIR" "$PROJECTS_DIR"; do
    if [ -d "$DIR" ]; then
      if [ -w "$DIR" ]; then
        echo -e "  ${GREEN}${CHECK_MARK}${NC} $DIR (writable)"
      else
        echo -e "  ${RED}${CROSS_MARK}${NC} $DIR (not writable)"
        CRITICAL_FAILURES=$((CRITICAL_FAILURES + 1))
      fi
    else
      echo -e "  ${YELLOW}${WARNING}${NC} $DIR (will be created on first run)"
    fi
  done
  echo ""
}

# Function to check network connectivity
check_network() {
  echo -n "Checking network connectivity... "

  if command -v curl &>/dev/null; then
    if curl -s --connect-timeout 5 https://www.google.com >/dev/null 2>&1; then
      echo -e "${GREEN}${CHECK_MARK} Online${NC}"
      echo "  Note: Network required for cloud AI features"
      echo ""
      return 0
    else
      echo -e "${YELLOW}${WARNING} Offline${NC}"
      echo "  Note: Cloud AI features will be unavailable"
      echo ""
      return 1
    fi
  else
    echo -e "${YELLOW}Unable to check (curl not found)${NC}"
    echo ""
    return 1
  fi
}

# Main dependency checks
echo "=== CRITICAL DEPENDENCIES ==="
echo ""

check_command "dotnet" ".NET Runtime" "true"
check_command "ffmpeg" "FFmpeg" "true"

echo ""
echo "=== OPTIONAL DEPENDENCIES ==="
echo ""

check_command "python" "Python" "false" || check_command "python3" "Python" "false"
check_command "node" "Node.js" "false"

echo ""
echo "=== PYTHON PACKAGES (OPTIONAL) ==="
echo ""

# Only check if Python is available
if command -v pip &>/dev/null || command -v pip3 &>/dev/null; then
  check_pip_package "torch" "PyTorch"
  check_pip_package "transformers" "Transformers"
  check_pip_package "openai-whisper" "OpenAI Whisper"
  check_pip_package "opencv-python" "OpenCV"
else
  echo "Python/pip not available - skipping package checks"
  echo ""
fi

echo ""
echo "=== HARDWARE ==="
echo ""

check_gpu

echo ""
echo "=== SYSTEM RESOURCES ==="
echo ""

check_disk_space
check_directories

echo ""
echo "=== NETWORK ==="
echo ""

check_network

echo ""
echo "================================================"
echo "  SUMMARY"
echo "================================================"
echo ""

if [ $CRITICAL_FAILURES -eq 0 ]; then
  echo -e "${GREEN}${CHECK_MARK} All critical dependencies met${NC}"
else
  echo -e "${RED}${CROSS_MARK} $CRITICAL_FAILURES critical dependency failures${NC}"
fi

if [ $OPTIONAL_MISSING -gt 0 ]; then
  echo -e "${YELLOW}${WARNING} $OPTIONAL_MISSING optional dependencies missing${NC}"
  echo "  Some features may be limited"
fi

if [ $WARNINGS -gt 0 ]; then
  echo -e "${YELLOW}${WARNING} $WARNINGS warnings${NC}"
fi

echo ""

# Exit code
if [ $CRITICAL_FAILURES -eq 0 ]; then
  echo -e "${GREEN}System ready for Aura Video Studio!${NC}"
  echo ""
  echo "Next steps:"
  echo "  1. Run the application"
  echo "  2. Complete the first-run wizard"
  echo "  3. Configure any missing optional dependencies for enhanced features"
  echo ""
  exit 0
else
  echo -e "${RED}Critical dependencies missing. Please install them before running Aura Video Studio.${NC}"
  echo ""
  echo "Installation guides:"
  echo "  - FFmpeg: See docs/FFmpeg_Setup_Guide.md"
  echo "  - .NET: https://dotnet.microsoft.com/download"
  echo ""
  exit 1
fi
