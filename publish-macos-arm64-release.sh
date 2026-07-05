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
dotnet publish -c Release -r osx-arm64 -o ./build/OreimoTranslateTool-macOS-ARM64 --self-contained 2>&1 | grep "OreimoTranslateToolAvalonia ->\|OreimoTranslateToolCLI ->"

# Create app bundles
echo ""
echo "Creating .app bundles..."
./create-macos-app-bundle.sh ./build/OreimoTranslateTool-macOS-ARM64 ARM64

# Create releases directory if it doesn't exist
mkdir -p ./releases

# Remove old release file if exists
if [ -f "releases/OreimoTranslateTool-macOS-ARM64.tar.gz" ]; then
    rm releases/OreimoTranslateTool-macOS-ARM64.tar.gz
fi

# Package everything
echo ""
echo "Packaging release..."
cd ./build
tar -czf ../releases/OreimoTranslateTool-macOS-ARM64.tar.gz OreimoTranslateTool-macOS-ARM64/
cd ..

# Display results
echo ""
echo "=== Build Complete ==="
echo ""
ls -lh releases/OreimoTranslateTool-macOS-ARM64.tar.gz
echo ""
echo "✅ macOS ARM64 release ready: releases/OreimoTranslateTool-macOS-ARM64.tar.gz"
echo ""
echo "Package contents:"
tar -tzf releases/OreimoTranslateTool-macOS-ARM64.tar.gz | head -20
echo "..."

