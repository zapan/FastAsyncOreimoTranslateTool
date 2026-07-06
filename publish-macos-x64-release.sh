#!/bin/bash
# Build script for macOS x64 release with .app bundles
set -e

echo "=== FastAsyncOreimoTranslateTool - macOS x64 Release Build ==="
echo ""

# Check if running from the correct directory
if [ ! -f "OreimoTranslateTool.sln" ]; then
    echo "Error: This script must be run from the project root directory"
    exit 1
fi

# Clean previous build
echo "Cleaning previous build..."
rm -rf ./build/OreimoTranslateTool-macOS-x64

# Publish both CLI and GUI to the same directory
echo "Publishing for macOS x64..."
dotnet publish -c Release -r osx-x64 -o ./build/OreimoTranslateTool-macOS-x64 --self-contained 2>&1 | grep "OreimoTranslateTool ->\|OreimoTranslateToolCLI ->"

# Create app bundles
echo ""
echo "Creating .app bundles..."
./create-macos-app-bundle.sh ./build/OreimoTranslateTool-macOS-x64 x64

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
  "./releases/OreimoTranslateTool-macOS-x64.dmg" \
  "./build/OreimoTranslateTool-macOS-x64/OreimoTranslateTool.app" 2>/dev/null

# Display results
echo ""
echo "=== Build Complete ==="
echo ""
ls -lh releases/OreimoTranslateTool-macOS-x64.dmg
echo ""
echo "✅ macOS x64 release ready: releases/OreimoTranslateTool-macOS-x64.dmg"
echo ""
