using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscUtils.Iso9660;

namespace RyuujiApi;

public class ProgressCounter {
    public int ProcessedFiles { get; set; }
    public int TotalFiles { get; set; }
}

public static class IsoTools {
    public static async Task ExtractIso(string isoPath, string outputDir, Action<string>? fileCallback = null, Action<byte>? progressCallback = null, CancellationToken ct = default) {
        if (string.IsNullOrWhiteSpace(isoPath))
            throw new ArgumentException("ISO path cannot be null or empty", nameof(isoPath));
        
        if (string.IsNullOrWhiteSpace(outputDir))
            throw new ArgumentException("Output directory cannot be null or empty", nameof(outputDir));

        Directory.CreateDirectory(outputDir);
        
        await using FileStream stream = new(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan);
        using CDReader reader = new(stream, true);

        int totalFiles = CountFiles("");
        var progress = new ProgressCounter { ProcessedFiles = 0, TotalFiles = totalFiles };
        
        await Task.Run(() => {
            ExtractDirectory("");
        });
        
        return;
        
        int CountFiles(string pathInIso) {
            int count = 0;
            foreach (string directory in reader.GetDirectories(pathInIso))
                count += CountFiles(directory);
            foreach (string file in reader.GetFiles(pathInIso))
                count++;
            return count;
        }
        
        void ExtractDirectory(string pathInIso) {
            foreach (string directory in reader.GetDirectories(pathInIso)) {
                string extractDir = Path.Join(outputDir, directory.Replace('\\', '/'));
                Directory.CreateDirectory(extractDir);
                ExtractDirectory(directory);
            }

            foreach (string file in reader.GetFiles(pathInIso)) {
                ExtractFile(file).GetAwaiter().GetResult();
            }
        }

        async Task ExtractFile(string file) {
            ct.ThrowIfCancellationRequested();
            fileCallback?.Invoke(file);
            string extractFilePath = Path.Join(outputDir, file.Replace('\\', '/'));
            Directory.CreateDirectory(Path.GetDirectoryName(extractFilePath)!);

            await using Stream isoStream = reader.OpenFile(file, FileMode.Open);
            await using FileStream outputFile = new(extractFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan);
            
            await isoStream.CopyToAsync(outputFile);
            
            progress.ProcessedFiles++;
            if (progressCallback != null && progress.TotalFiles > 0) {
                byte p = (byte)((float)progress.ProcessedFiles / progress.TotalFiles * 100);
                progressCallback(p);
            }
        }
    }
    
    public static void RepackIso(string mkisofsPath, string inputDir, string outputPath, Action<float>? progressCallback = null) {
        if (!File.Exists(mkisofsPath)) {
            // Try to find mkisofs in PATH
            mkisofsPath = FindExecutable("mkisofs");
            if (string.IsNullOrEmpty(mkisofsPath)) {
                throw new FileNotFoundException("mkisofs not found. Install it with: brew install cdrtools on macOS", "mkisofs");
            }
        }
        
        if (!Directory.Exists(inputDir)) {
            throw new DirectoryNotFoundException($"Input directory not found: {inputDir}");
        }
        
        var psi = new ProcessStartInfo {
            FileName = mkisofsPath,
            Arguments = $"-f -J -l -no-pad -o \"{outputPath}\" \"{inputDir}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        using var process = new Process();
        process.StartInfo = psi;
        
        process.OutputDataReceived += (s, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                Console.WriteLine(e.Data);
            }
        };
        
        process.ErrorDataReceived += (s, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                Console.WriteLine($"Error: {e.Data}");
            }
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        
        if (process.ExitCode != 0) {
            throw new InvalidOperationException($"mkisofs failed with exit code: {process.ExitCode}");
        }
        
        progressCallback?.Invoke(100f);
    }
    
    private static string? FindExecutable(string executableName) {
        // Check if it's in PATH
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
        if (paths != null) {
            foreach (var path in paths) {
                var fullPath = Path.Combine(path, executableName);
                if (File.Exists(fullPath)) {
                    return fullPath;
                }
            }
        }
        return null;
    }
}
