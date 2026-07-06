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

# Remove old release file if exists
if [ -f "releases/OreimoTranslateTool-macOS-x64.tar.gz" ]; then
    rm releases/OreimoTranslateTool-macOS-x64.tar.gz
fi

# Package everything
echo ""
echo "Packaging release..."
cd ./build
tar -czf ../releases/OreimoTranslateTool-macOS-x64.tar.gz OreimoTranslateTool-macOS-x64/
cd ..

# Display results
echo ""
echo "=== Build Complete ==="
echo ""
ls -lh releases/OreimoTranslateTool-macOS-x64.tar.gz
echo ""
echo "✅ macOS x64 release ready: releases/OreimoTranslateTool-macOS-x64.tar.gz"
echo ""
echo "Package contents:"
tar -tzf releases/OreimoTranslateTool-macOS-x64.tar.gz | head -20
echo "..."

