# Resources Directory

This directory contains bundled resources for the Aura Video Studio desktop application.

## Directory Structure

- **backend/** - Built .NET backend binaries (auto-generated during build)
  - `win-x64/` - Windows x64 backend
  - `osx-x64/` - macOS x64 backend
  - `osx-arm64/` - macOS ARM64 backend
  - `linux-x64/` - Linux x64 backend

## Build Process

The `build-desktop.ps1` and `build-desktop.sh` scripts automatically build the backend and place it in this directory.

**Do not manually place files here** - the build scripts will overwrite them.

## Optional Resources

The following resources are optional and can be added if needed:

- **ffmpeg/** - FFmpeg binaries for video processing (the app can download these at runtime)
- **samples/** - Sample project files

These optional resources are not included by default to keep the installer size smaller.
