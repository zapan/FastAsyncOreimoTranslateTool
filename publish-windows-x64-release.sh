#!/bin/bash
# Build script for Windows x64 release with .app bundles
set -e

echo "=== FastAsyncOreimoTranslateTool - Windows x64 Release Build ==="
echo ""

# Check if running from the correct directory
if [ ! -f "OreimoTranslateTool.sln" ]; then
    echo "Error: This script must be run from the project root directory"
    exit 1
fi

# Clean previous build
echo "Cleaning previous build..."
rm -rf ./build/OreimoTranslateTool-Windows-x64

# Publish both CLI and GUI to the same directory
echo "Publishing for Windows x64..."
dotnet publish OreimoTranslateToolCLI -c Release -r win-x64 -o ./build/OreimoTranslateTool-Windows-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish OreimoTranslateTool -c Release -r win-x64 -o ./build/OreimoTranslateTool-Windows-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

#StartGame for windows
cp ./RyuujiApi/Resources/StartGame.Windows.conf ./build/OreimoTranslateTool-Windows-x64/Resources/StartGame.conf

# Create releases directory if it doesn't exist
mkdir -p ./releases

# Remove old release file if exists
if [ -f "releases/OreimoTranslateTool-Windows-x64.tar.gz" ]; then
    rm releases/OreimoTranslateTool-Windows-x64.tar.gz
fi

# Package everything
echo ""
echo "Packaging release..."
cd ./build
tar -czf ../releases/OreimoTranslateTool-Windows-x64.tar.gz OreimoTranslateTool-Windows-x64/
cd ..

# Display results
echo ""
echo "=== Build Complete ==="
echo ""
ls -lh releases/OreimoTranslateTool-Windows-x64.tar.gz
echo ""
echo "✅ Windows x64 release ready: releases/OreimoTranslateTool-Windows-x64.tar.gz"
echo ""
echo "Package contents:"
tar -tzf releases/OreimoTranslateTool-Windows-x64.tar.gz | head -20
echo "..."

