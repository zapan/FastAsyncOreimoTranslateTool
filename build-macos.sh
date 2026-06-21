#!/bin/bash
# Build script for macOS
set -e

echo "=== FastAsyncToradoraTranslateTool - macOS Build Script ==="

# Check if running on macOS
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "Warning: This script is designed for macOS"
    read -p "Are you sure you want to continue? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Check for required tools
echo "Checking dependencies..."

if ! command -v dotnet &> /dev/null; then
    echo "Error: dotnet SDK not found. Please install from https://dotnet.microsoft.com/download"
    exit 1
fi

if ! command -v mkisofs &> /dev/null; then
    echo "Warning: mkisofs not found. Installing with Homebrew..."
    if command -v brew &> /dev/null; then
        brew install cdrtools
    else
        echo "Error: Homebrew not found. Please install mkisofs manually or install Homebrew"
        exit 1
    fi
fi

# Make scripts executable
echo "Making scripts executable..."
chmod +x Resources/!!Tools/DatWorker/gzip

# Restore packages
echo "Restoring packages..."
dotnet restore

# Build the solution
echo "Building solution..."
dotnet build -c Release

# Build the CLI
echo "Building CLI..."
dotnet build ToradoraTranslateToolCLI -c Release

echo ""
echo "=== Build Complete ==="
echo "To run the CLI: dotnet run --project ToradoraTranslateToolCLI"
echo ""
echo "Make sure to set up mkisofs.conf if mkisofs is not in your PATH:"
echo "  echo 'path/to/mkisofs' > mkisofs.conf"
