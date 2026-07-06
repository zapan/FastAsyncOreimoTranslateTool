#!/bin/bash
# Build script for macOS ARM64 release with .app bundles
set -e

echo "=== FastAsyncOreimoTranslateTool - macOS ARM64 Release Build ==="
echo ""

# Check if running from the correct directory
if [ ! -f "OreimoTranslateTool.sln" ]; then
    echo "Error: This script must be run from the project root directory"
    exit 1
fi

# Clean previous build
echo "Cleaning previous build..."
rm -rf ./build/OreimoTranslateTool-macOS-ARM64

# Publish both CLI and GUI to the same directory
echo "Publishing for macOS ARM64..."
dotnet publish -c Release -r osx-arm64 -o ./build/OreimoTranslateTool-macOS-ARM64 --self-contained 2>&1 | grep "OreimoTranslateTool ->\|OreimoTranslateToolCLI ->"

# Create app bundles
echo ""
echo "Creating .app bundles..."
./create-macos-app-bundle.sh ./build/OreimoTranslateTool-macOS-ARM64 ARM64

# Create releases directory if it doesn't exist
mkdir -p ./releases

# Package everything
echo ""
echo "Packaging release..."
create-dmg \
  --overwrite \
  --volname "Oreimo Translate Tool" \
  --window-pos 200 120 \
  --window-size 600 400 \
  --icon-size 100 \
  --icon "OreimoTranslateTool.app" 175 120 \
  --hide-extension "OreimoTranslateTool.app" \
  --app-drop-link 425 120 \
  "./releases/OreimoTranslateTool-macOS-ARM64.dmg" \
  "./build/OreimoTranslateTool-macOS-ARM64/OreimoTranslateTool.app" 2>/dev/null

# Display results
echo ""
echo "=== Build Complete ==="
echo ""
ls -lh releases/OreimoTranslateTool-macOS-ARM64.dmg
echo ""
echo "✅ macOS ARM64 release ready: releases/OreimoTranslateTool-macOS-ARM64.dmg"
echo ""
