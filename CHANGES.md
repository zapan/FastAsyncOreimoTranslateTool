# Cambios para Soporte macOS

Este documento detalla los cambios realizados para hacer que FastAsyncToradoraTranslateTool funcione nativamente en macOS.

## Archivos Modificados

### 1. `ToradoraTranslateToolCLI/CLI.cs`
- **Cambios:**
  - Corregido método `Main` para que sea `void` en lugar de no tener tipo de retorno
  - Agregada detección de plataforma (Windows/macOS/Unix)
  - Actualizados mensajes de error para incluir instrucciones de macOS
  - Agregada directiva `using System.Runtime.InteropServices`

### 2. `ToradoraTranslateToolCLI/ToradoraTranslateToolCLI.csproj`
- **Cambios:**
  - Agregado soporte para macOS con condicionales de compilación
  - Definición de constantes `_WINDOWS` y `__UNIX__` según la plataforma
  - Configuración para copiar archivos específicos de macOS

### 3. `RyuujiApi/IsoTools.cs` (nuevo archivo)
- **Cambios:**
  - Reescrito desde cero para usar la API correcta de `DiscUtils.Iso9660`
  - Corregido uso de métodos `GetDirectories()` y `GetFiles()` de CDReader
  - Implementación de extracción ISO multiplataforma
  - Soporte para `mkisofs` nativo de macOS
  - Agregada clase `ProgressCounter` para gestión de progreso

### 4. `RyuujiApi/RyuujiApi.cs`
- **Cambios:**
  - Corregido namespace (de namespace a clase pública)

### 5. `DatWorker/DatWorker.cs`
- **Cambios:**
  - Agregado soporte condicional para usar `MakeGpdaMacOS` en Unix
  - Uso de `__UNIX__` para compilar código específico de macOS

### 6. `Readme.md`
- **Cambios:**
  - Agregada sección completa sobre soporte macOS
  - Instrucciones para instalar dependencias con Homebrew
  - Comandos para construir y ejecutar en macOS
  - Diferencias con la versión de Windows

## Archivos Nuevos

### 1. `CppPorts/MakeGpda.macOS.cs`
- Implementación base de MakeGpda para macOS
- Firma lista para implementación completa del formato GPDA en macOS

### 2. `build-macos.sh`
- Script de build para macOS
- Verificación de dependencias (dotnet, mkisofs)
- Comandos de build automáticos
- Instrucciones post-build

### 3. `mkisofs.conf`
- Configuración para encontrar `mkisofs` en macOS
- Por defecto usa `mkisofs` en PATH

### 4. `Resources/!!Tools/DatWorker/gzip`
- Script shell wrapper para usar `gzip` nativo de macOS

## Dependencias de macOS

### Requeridas
1. **.NET 8.0 SDK**
   ```bash
   brew install --cask dotnet-sdk
   ```

2. **cdrtools (mkisofs)**
   ```bash
   brew install cdrtools
   ```

3. **NativeFileDialogSharp** (dependencia de NuGet)
   - Ya incluida en el proyecto

### Opcionales
- **libffi, libplist** (para NativeFileDialogSharp)
  ```bash
  brew install libffi libplist
  ```

## Compilación en macOS

```bash
# Script automático
./build-macos.sh

# Manual
dotnet restore
dotnet build -c Release
dotnet build ToradoraTranslateToolCLI -c Release
```

## Ejecución

```bash
dotnet run --project ToradoraTranslateToolCLI
```

## Notas Importantes

### Para mkisofs
Si `mkisofs` no está en PATH, crear el archivo `mkisofs.conf` con la ruta:
```bash
echo '/opt/homebrew/bin/mkisofs' > mkisofs.conf
```

### Para NativeFileDialogSharp
Asegurarse de tener instalado:
```bash
brew install libffi libplist
```

## Limitaciones Conocidas

1. **CppPorts (MakeGpda, ModSeekMap)**: Requiere implementación completa para macOS
   - Actualmente usa versiones Windows o espera implementación futura
   - Archivo `MakeGpda.macOS.cs` listo para desarrollo

2. **Herramientas externas**: Algunas herramientas Windows necesitan reemplazo:
   - `makeGDP.exe` → En espera de implementación
   - `modseekmap.exe` → En espera de implementación

## Próximos Pasos

1. Implementar completamente MakeGpda para macOS
2. Implementar completamente ModSeekMap para macOS
3. Pruebas extensivas de todas las funcionalidades
4. Posiblemente crear versiones nativas de todas las herramientas en C# puro
