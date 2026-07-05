#!/bin/bash
# Build script for Windows ARM64 release with .app bundles
set -e

echo "=== FastAsyncOreimoTranslateTool - Windows ARM64 Release Build ==="
echo ""

# Check if running from the correct directory
if [ ! -f "OreimoTranslateTool.sln" ]; then
    echo "Error: This script must be run from the project root directory"
    exit 1
fi

# Clean previous build
echo "Cleaning previous build..."
rm -rf ./build/OreimoTranslateTool-Windows-ARM64

# Publish both CLI and GUI to the same directory
echo "Publishing for Windows ARM64..."
dotnet publish OreimoTranslateToolCLI -c Release -r win-arm64 -o ./build/OreimoTranslateTool-Windows-ARM64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish OreimoTranslateToolAvalonia -c Release -r win-arm64 -o ./build/OreimoTranslateTool-Windows-ARM64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Create releases directory if it doesn't exist
mkdir -p ./releases

# Remove old release file if exists
if [ -f "releases/OreimoTranslateTool-Windows-ARM64.tar.gz" ]; then
    rm releases/OreimoTranslateTool-Windows-ARM64.tar.gz
fi

# Package everything
echo ""
echo "Packaging release..."
cd ./build
tar -czf ../releases/OreimoTranslateTool-Windows-ARM64.tar.gz OreimoTranslateTool-Windows-ARM64/
cd ..

# Display results
echo ""
echo "=== Build Complete ==="
echo ""
ls -lh releases/OreimoTranslateTool-Windows-ARM64.tar.gz
echo ""
echo "✅ Windows ARM64 release ready: releases/OreimoTranslateTool-Windows-ARM64.tar.gz"
echo ""
echo "Package contents:"
tar -tzf releases/OreimoTranslateTool-Windows-ARM64.tar.gz | head -20
echo "..."

