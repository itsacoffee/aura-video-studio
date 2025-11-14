#!/bin/bash
# Install FFmpeg for Aura Video Studio (Linux)
#
# This script downloads and installs FFmpeg to the Aura dependencies folder
# Usage: bash install-ffmpeg-linux.sh [--source=static|system]

set -e

# Configuration
SOURCE="static" # Options: "static" (static build), "system" (package manager)
DEST_PATH="$HOME/.local/share/Aura/dependencies/bin"

# Parse command line arguments
for arg in "$@"; do
  case $arg in
    --source=*)
      SOURCE="${arg#*=}"
      shift
      ;;
    --dest=*)
      DEST_PATH="${arg#*=}"
      shift
      ;;
    --help | -h)
      echo "Usage: $0 [options]"
      echo ""
      echo "Options:"
      echo "  --source=static|system    Installation method (default: static)"
      echo "  --dest=PATH               Destination path (default: ~/.local/share/Aura/dependencies/bin)"
      echo "  --help                    Show this help message"
      echo ""
      echo "Examples:"
      echo "  $0                        # Install static build"
      echo "  $0 --source=system        # Install via package manager"
      exit 0
      ;;
  esac
done

echo "======================================"
echo "FFmpeg Installer for Aura Video Studio"
echo "======================================"
echo ""

# Detect Linux distribution
if [ -f /etc/os-release ]; then
  . /etc/os-release
  DISTRO=$ID
else
  DISTRO="unknown"
fi

echo "Detected distribution: $DISTRO"
echo "Installation method: $SOURCE"
echo ""

install_via_package_manager() {
  echo "Installing FFmpeg via package manager..."
  echo ""

  case $DISTRO in
    ubuntu | debian | linuxmint | pop)
      echo "Using apt..."
      sudo apt update
      sudo apt install -y ffmpeg
      ;;
    fedora)
      echo "Using dnf..."
      sudo dnf install -y ffmpeg
      ;;
    arch | manjaro)
      echo "Using pacman..."
      sudo pacman -S --noconfirm ffmpeg
      ;;
    opensuse*)
      echo "Using zypper..."
      sudo zypper install -y ffmpeg
      ;;
    *)
      echo "Error: Unsupported distribution for automatic installation"
      echo "Please install FFmpeg manually or use --source=static"
      exit 1
      ;;
  esac

  # Verify installation
  if command -v ffmpeg &>/dev/null; then
    echo ""
    echo "✅ FFmpeg installed successfully via package manager!"
    ffmpeg -version | head -1
    echo ""
    echo "FFmpeg is now in your system PATH and will be detected by Aura."
    return 0
  else
    echo "Error: FFmpeg installation failed"
    return 1
  fi
}

install_static_build() {
  echo "Installing FFmpeg static build..."
  echo ""

  # Detect architecture
  ARCH=$(uname -m)
  case $ARCH in
    x86_64)
      ARCH="amd64"
      ;;
    aarch64 | arm64)
      ARCH="arm64"
      ;;
    armv7l)
      ARCH="armhf"
      ;;
    i686 | i386)
      ARCH="i686"
      ;;
    *)
      echo "Error: Unsupported architecture: $ARCH"
      exit 1
      ;;
  esac

  echo "Architecture: $ARCH"
  echo ""

  # Download URL
  DOWNLOAD_URL="https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-${ARCH}-static.tar.xz"
  TEMP_DIR=$(mktemp -d)
  DOWNLOAD_FILE="$TEMP_DIR/ffmpeg.tar.xz"

  echo "[1/5] Downloading FFmpeg static build..."
  echo "From: $DOWNLOAD_URL"
  echo "To: $DOWNLOAD_FILE"

  if command -v wget &>/dev/null; then
    wget -q --show-progress "$DOWNLOAD_URL" -O "$DOWNLOAD_FILE"
  elif command -v curl &>/dev/null; then
    curl -L --progress-bar "$DOWNLOAD_URL" -o "$DOWNLOAD_FILE"
  else
    echo "Error: wget or curl is required but not found"
    exit 1
  fi

  echo "✅ Download complete"
  echo ""

  echo "[2/5] Extracting archive..."
  tar xf "$DOWNLOAD_FILE" -C "$TEMP_DIR"
  echo "✅ Extraction complete"
  echo ""

  echo "[3/5] Locating binaries..."
  EXTRACT_DIR=$(find "$TEMP_DIR" -maxdepth 1 -type d -name "ffmpeg-*" | head -1)
  if [ -z "$EXTRACT_DIR" ]; then
    echo "Error: Could not find extracted directory"
    rm -rf "$TEMP_DIR"
    exit 1
  fi

  echo "Found in: $EXTRACT_DIR"
  ls -lh "$EXTRACT_DIR"/ffmpeg "$EXTRACT_DIR"/ffprobe 2>/dev/null || true
  echo ""

  echo "[4/5] Installing to Aura dependencies..."
  echo "Destination: $DEST_PATH"

  # Create destination directory
  mkdir -p "$DEST_PATH"

  # Copy binaries
  cp "$EXTRACT_DIR/ffmpeg" "$DEST_PATH/"
  cp "$EXTRACT_DIR/ffprobe" "$DEST_PATH/"

  # Make executable
  chmod +x "$DEST_PATH/ffmpeg"
  chmod +x "$DEST_PATH/ffprobe"

  echo "✅ Installation complete"
  echo ""

  echo "[5/5] Verifying installation..."
  VERSION=$("$DEST_PATH/ffmpeg" -version 2>&1 | head -1)

  if [[ $VERSION == ffmpeg\ version* ]]; then
    echo "✅ FFmpeg is working correctly!"
    echo ""
    echo "Version: $VERSION"
  else
    echo "Error: FFmpeg verification failed"
    rm -rf "$TEMP_DIR"
    exit 1
  fi

  # Cleanup
  echo ""
  echo "Cleaning up temporary files..."
  rm -rf "$TEMP_DIR"
  echo "✅ Cleanup complete"
}

# Main installation
case $SOURCE in
  static)
    install_static_build
    ;;
  system)
    install_via_package_manager
    ;;
  *)
    echo "Error: Invalid source '$SOURCE'. Valid options: static, system"
    exit 1
    ;;
esac

# Success message
echo ""
echo "======================================"
echo "Installation completed successfully!"
echo "======================================"
echo ""
echo "FFmpeg installed to:"

if [ "$SOURCE" = "system" ]; then
  echo "  System PATH (use 'which ffmpeg' to find location)"
else
  echo "  $DEST_PATH"
fi

echo ""
echo "Next steps:"
echo "  1. Open Aura Video Studio"
echo "  2. Go to Download Center → Engines tab"
echo "  3. Click 'Rescan' on the FFmpeg card"
echo "  4. FFmpeg should be detected automatically!"
echo ""
