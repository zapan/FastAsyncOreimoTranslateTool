#!/bin/bash
# Create macOS .app bundles for OreimoTranslateTool
set -e

echo "=== Creating macOS .app Bundles ==="
echo ""

# Function to create an app bundle
create_app_bundle() {
    local app_name=$1
    local executable_name=$2
    local build_dir=$3
    local bundle_name="${app_name}.app"
    local bundle_path="$build_dir/$bundle_name"
    
    echo "Creating $bundle_name..."
    
    # Create bundle structure
    mkdir -p "$bundle_path/Contents/MacOS"
    mkdir -p "$bundle_path/Contents/Resources"
    
    # Copy executable
    cp "$build_dir/$executable_name" "$bundle_path/Contents/MacOS/"
    chmod +x "$bundle_path/Contents/MacOS/$executable_name"
    
    # Create a wrapper script that sets up the environment
    cat > "$bundle_path/Contents/MacOS/launcher.sh" << 'EOF'
#!/bin/bash
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export DOTNET_ROOT="$DIR/../Resources"
export LD_LIBRARY_PATH="$DIR/../Resources:$LD_LIBRARY_PATH"
exec "$DIR/../Resources/executable_placeholder" "$@"
EOF
    
    sed -i '' "s|executable_placeholder|$executable_name|g" "$bundle_path/Contents/MacOS/launcher.sh"
    chmod +x "$bundle_path/Contents/MacOS/launcher.sh"
    
    # Move the actual executable and create a symlink/wrapper
    mv "$bundle_path/Contents/MacOS/$executable_name" "$bundle_path/Contents/Resources/$executable_name"
    chmod +x "$bundle_path/Contents/Resources/$executable_name"
    
    # Copy icon
    cp OreimoTranslateTool/Assets/AppIcon.icns "$bundle_path/Contents/Resources/AppIcon.icns"
    
    # Copy Assets and Data
    cp -r RyuujiApi/Resources "$bundle_path/Contents/Resources/" 2>/dev/null || true
    echo "***********************************************"
    
    # Copy ALL .NET files (dlls, so, dylib, pdb, configs, etc.) from original build dir to Resources
    # Use cp instead of mv so both bundles get the files
    find "$build_dir" -maxdepth 1 -type f \( -name "*.dll" -o -name "*.so" -o -name "*.dylib" -o -name "*.pdb" -o -name "*.runtimeconfig.json" -o -name "*.deps.json" -o -name "createdump" \) -exec cp {} "$bundle_path/Contents/Resources/" \;
    
    # Create Info.plist
    local version=$(date +%Y.%m.%d)
    cat > "$bundle_path/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>launcher.sh</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleIdentifier</key>
    <string>com.oreimo.$(echo $app_name | tr ' ' '.')</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$app_name</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.6.2</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © 2026. All rights reserved.</string>
    <key>NSRequiresIPhoneOS</key>
    <false/>
</dict>
</plist>
EOF
    
    echo "✓ $bundle_name created"
}

# Check arguments
if [ $# -lt 1 ]; then
    echo "Usage: $0 <build-directory> [architecture]"
    echo "Example: $0 ./build/OreimoTranslateTool-macOS-ARM64 ARM64"
    exit 1
fi

build_dir=$1
arch=${2:-"ARM64"}

if [ ! -d "$build_dir" ]; then
    echo "Error: Build directory '$build_dir' not found"
    exit 1
fi

echo "Processing: $build_dir (Architecture: $arch)"
echo ""

# Create bundles
create_app_bundle "OreimoTranslateTool" "OreimoTranslateToolCLI" "$build_dir"
create_app_bundle "OreimoTranslateTool" "OreimoTranslateTool" "$build_dir"

# Clean up individual executables (they're now inside the bundles)
rm -f "$build_dir/OreimoTranslateTool"
rm -f "$build_dir/OreimoTranslateToolCLI"

# Remove other CLI tools that we don't need in the release
rm -f "$build_dir/DatWorker.Cli"
rm -f "$build_dir/MakeGpda.Cli"
rm -f "$build_dir/ModSeekmap.Cli"
rm -f "$build_dir/TenoriTool.Cli"
rm -f "$build_dir/TranslateCLI"

# Remove loose asset folders and data (now inside bundles)
rm -rf "$build_dir/Assets"
rm -rf "$build_dir/Data"
rm -rf "$build_dir/Resources"

# Remove .NET files from root (they're now inside the bundles)
find "$build_dir" -maxdepth 1 -type f \( -name "*.dll" -o -name "*.so" -o -name "*.dylib" -o -name "*.pdb" -o -name "*.runtimeconfig.json" -o -name "*.deps.json" -o -name "createdump" \) -delete

# Remove macOS system files
rm -f "$build_dir/.DS_Store"

echo ""
echo "=== App Bundles Created Successfully ==="
echo ""
echo "Contents of $build_dir:"
ls -lh "$build_dir"
