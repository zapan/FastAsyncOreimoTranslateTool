# Changelog

All notable changes to FastAsyncOreimoTranslateTool are documented in this file.

## [v1.0.0] - 2026-07-04

### 🎉 Major Features

#### Cross-Platform GUI (Avalonia)
- ✨ Ported original GUI to **Avalonia** framework for cross-platform support
- 🍎 **Native macOS support** (x64 and ARM64)
- 🪟 **Native Windows support** (x64 and ARM64)  
- 🐧 **Linux support** included
- Unified codebase for all platforms replacing platform-specific implementations

#### Extended CLI
- 📝 CLI tool extended with full macOS support
- 🔧 New standalone options for automation and scripting
- 🎯 Better integration for headless operations and batch processing

#### macOS Support
- Full native support for macOS (Intel and Apple Silicon)
- Platform-specific implementations using native tools (gzip, mkisofs)
- Proper handling of macOS file system and architecture differences

#### GitHub Actions Workflow
- 🚀 Automated CI/CD pipeline for building releases
- 📦 Automatic binary generation for macOS (x64, ARM64) and Windows (x64, ARM64)
- 🔄 Self-contained executables with no .NET runtime dependency needed
- 📋 Automated release creation with all binaries

### 📝 Project Rebranding
- Renamed from **ToradoraTranslateTool** to **OreimoTranslateTool**
- Updated solution file and all project references
- Updated tool titles and branding throughout the application
- Maintains compatibility with original project structure

### 🔧 Modified Files

#### Core CLI Changes
**`OreimoTranslateToolCLI/CLI.cs`**
- Fixed `Main` method signature
- Added platform detection (Windows/macOS/Unix)
- Updated error messages with macOS-specific instructions
- Added `System.Runtime.InteropServices` support

**`OreimoTranslateToolCLI/OreimoTranslateToolCLI.csproj`**
- Added macOS compilation conditionals
- Platform-specific constants (`_WINDOWS`, `__UNIX__`)
- Configuration for copying platform-specific files

#### API and Worker Updates
**`RyuujiApi/IsoTools.cs`**
- Rewrote for correct `DiscUtils.Iso9660` API usage
- Fixed `GetDirectories()` and `GetFiles()` methods for CDReader
- Cross-platform ISO extraction implementation
- Native macOS `mkisofs` support
- Added `ProgressCounter` class for progress management

**`RyuujiApi/RyuujiApi.cs`**
- Corrected namespace to public class

**`DatWorker/DatWorker.cs`**
- Conditional support for macOS via `MakeGpdaMacOS`
- `__UNIX__` compilation flags for platform-specific code

**`TranslationWindow.axaml.cs`**
- Fixed `basePath` parameter passing to `NamesWindow`
- Proper field initialization for cross-platform compatibility

#### Documentation
**`Readme.md`**
- Added comprehensive macOS section with prerequisites and build instructions
- Separated GUI (Avalonia) and CLI documentation
- Updated screenshots and feature list
- Added macOS-specific platform differences
- Updated Special Thanks to credit original projects

### 📦 New Files

#### GUI
**`OreimoTranslateToolAvalonia/`** (entire project)
- New Avalonia-based cross-platform GUI
- Replaces Windows Forms implementation
- Full macOS native support with proper file dialogs and UI conventions

#### Build and Configuration
**`build-macos.sh`**
- Automated macOS build script
- Dependency verification (dotnet, mkisofs)
- Multi-project build orchestration
- Post-build instructions

**`.github/workflows/dotnet.yml`**
- GitHub Actions CI/CD workflow
- Multi-platform builds (macOS x64/ARM64, Windows x64/ARM64)
- Automated release creation with binaries
- Self-contained executable generation

**`mkisofs.conf`**
- Configuration for macOS `mkisofs` path resolution
- Defaults to system PATH

**`Resources/!!Tools/DatWorker/gzip`**
- Shell wrapper for native macOS gzip

#### Platform Support
**`CppPorts/MakeGpda.macOS.cs`**
- Base implementation for macOS MakeGpda
- Ready for full GPDA format implementation

### 🛠️ macOS Setup Requirements

#### Required Dependencies
1. **.NET 8.0 SDK**
   ```bash
   brew install --cask dotnet-sdk
   ```

2. **cdrtools (mkisofs)**
   ```bash
   brew install cdrtools
   ```

#### Build Instructions

```bash
# Automatic (using provided script)
chmod +x Resources/!!Tools/DatWorker/gzip
./build-macos.sh

# Manual build
dotnet restore
dotnet build -c Release

# Build specific projects
dotnet build OreimoTranslateToolAvalonia -c Release
dotnet build OreimoTranslateToolCLI -c Release
```

#### Running Applications

```bash
# GUI (Avalonia)
dotnet run --project OreimoTranslateToolAvalonia

# CLI
dotnet run --project OreimoTranslateToolCLI
```

### ⚙️ Configuration

**mkisofs Path Configuration**
If `mkisofs` is not in your PATH, create `mkisofs.conf`:
```bash
echo '/opt/homebrew/bin/mkisofs' > mkisofs.conf
```

### 🐛 Known Limitations

1. **CppPorts (MakeGpda, ModSeekMap)**: Full macOS implementation pending
   - Currently uses Windows versions or awaits native implementation
   - `MakeGpda.macOS.cs` ready for development

2. **External Tools**: Some Windows executables need macOS replacements
   - `makeGDP.exe` → Implementation pending
   - `modseekmap.exe` → Implementation pending

### 📊 Performance (Inherited from FastAsyncToradoraTranslateTool)
- Game file extraction: **1,848.80%** faster than original
- Game file repacking: **2,037.40%** faster than original

### 🙏 Credits

Based on [FastAsyncToradoraTranslateTool](https://github.com/computer-catt/FastAsyncToradoraTranslateTool) by [computer-catt](https://github.com/computer-catt).

### 🔮 Future Plans

1. Complete macOS implementations for MakeGpda and ModSeekMap
2. Extensive testing of all functionality
3. Consider pure C# implementations of native tools
4. Additional platform optimizations
