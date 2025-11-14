#!/bin/bash
# Documentation build and validation script

set -e

echo "=== Aura Video Studio Documentation Builder ==="
echo ""

# Check if DocFX is installed
if ! command -v docfx &>/dev/null; then
  echo "Installing DocFX..."
  dotnet tool install -g docfx
fi

# Build .NET solution with XML documentation
echo "Building .NET solution..."
dotnet build --configuration Release

# Build DocFX documentation
echo "Building API documentation with DocFX..."
docfx docfx.json

# Build TypeScript documentation (if Node.js is available)
if command -v npm &>/dev/null; then
  echo "Building TypeScript documentation..."
  cd Aura.Web
  npm install --silent
  npm run docs
  cd ..
else
  echo "Skipping TypeScript documentation (npm not found)"
fi

# Validate links (if markdown-link-check is installed)
if command -v markdown-link-check &>/dev/null; then
  echo "Validating links in documentation..."
  FAILED_FILES=0
  while IFS= read -r -d '' file; do
    if ! markdown-link-check --quiet "$file"; then
      echo "Warning: Found broken links in $file"
      ((FAILED_FILES++))
    fi
  done < <(find docs -name "*.md" -print0)

  if [ $FAILED_FILES -gt 0 ]; then
    echo "Warning: $FAILED_FILES file(s) have broken links (not failing build)"
  fi
else
  echo "Skipping link validation (markdown-link-check not installed)"
  echo "Install with: npm install -g markdown-link-check"
fi

echo ""
echo "=== Documentation built successfully! ==="
echo "View at: file://$(pwd)/_site/index.html"
echo ""
echo "To serve locally:"
echo "  docfx serve _site"
echo ""
