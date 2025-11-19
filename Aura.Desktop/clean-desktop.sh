#!/bin/bash
# Shell Cleanup Script for Aura Video Studio Desktop
# This is a Unix/Linux/macOS wrapper that calls the PowerShell script
# For Windows users, use clean-desktop.ps1 directly

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}Aura Video Studio - Desktop Cleanup${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

# Check if PowerShell is available
if command -v pwsh &> /dev/null; then
    echo -e "${GREEN}[INFO] Using PowerShell Core (pwsh)${NC}"
    exec pwsh -NoProfile -File "$SCRIPT_DIR/clean-desktop.ps1" "$@"
elif command -v powershell &> /dev/null; then
    echo -e "${GREEN}[INFO] Using Windows PowerShell${NC}"
    exec powershell -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/clean-desktop.ps1" "$@"
else
    echo -e "${RED}[ERROR] PowerShell not found${NC}"
    echo ""
    echo "This script requires PowerShell to run."
    echo ""
    echo "Options:"
    echo "  1. Install PowerShell Core: https://aka.ms/powershell"
    echo "  2. On Windows, use clean-desktop.ps1 directly"
    echo "  3. Manual cleanup: Remove the following directories:"
    echo "     - ~/.config/aura-video-studio (Linux/macOS)"
    echo "     - ~/.cache/aura-video-studio"
    echo "     - /tmp/aura-video-studio"
    echo ""
    exit 1
fi
