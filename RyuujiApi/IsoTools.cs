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
    /*public static void ExtractIso(string isoFolder, string isoPath, Action<byte> progressCallback = null) {
        if (!Directory.Exists(isoFolder))
            Directory.CreateDirectory(isoFolder);



        SevenZipExtractor mySze = new(isoPath);
        if (progressCallback != null)
            mySze.Extracting += (_, args) => { progressCallback(args.PercentDone); };

        mySze.ExtractArchive(isoFolder);
        mySze.Dispose();
    }*/

    /*public static void RepackIso(string mkisoFsPath, string isoDirectory, string outputPath, Action<float> progressCallback = null) {
        string command = "-iso-level 4 -xa -A \"PSP GAME\" -V \"Toradora\" -sysid \"PSP GAME\" -volset \"Toradora\" -p \"\" -publisher \"\" -o \"" + outputPath + "\" \"" + isoDirectory + "\"";
        using Process myProc = new();
        myProc.StartInfo.FileName = Path.Combine(mkisoFsPath, "mkisofs.exe");
        myProc.StartInfo.Arguments = command;
        myProc.StartInfo.CreateNoWindow = true;
        myProc.StartInfo.WorkingDirectory = mkisoFsPath;
        myProc.StartInfo.RedirectStandardError = progressCallback != null;
        myProc.StartInfo.UseShellExecute = false;

        if (progressCallback != null)
            myProc.ErrorDataReceived += (_, args) => {
                /*Console.WriteLine(args.Data);#1#
                if (args.Data == null) return;
                if (float.TryParse(args.Data.Trim().Split(' ')[0].Trim('%'), out float result))
                    progressCallback(Math.Clamp(100, 0, result));
            };

        myProc.Start();
        if (progressCallback != null) myProc.BeginErrorReadLine();
        myProc.WaitForExit();
    }*/

    /*public static async Task RepackZip(string isoDirectory, string outputPath, Action<float> progressCallback = null) {
        SevenZipCompressor compressor = new() {
            ArchiveFormat = OutArchiveFormat.Zip,
            CompressionMethod = CompressionMethod.Copy,
            CompressionLevel = CompressionLevel.Fast,
        };
        compressor.CustomParameters.Add("mt", "on");
        if (progressCallback != null) compressor.Compressing += (_, args) => { progressCallback(args.PercentDone); };
        await compressor.CompressDirectoryAsync(isoDirectory, outputPath);
    }*/

    // this fucker caused me anguish :3
    // its not data safe, and only like 2 seconds quicker

    /*public static async Task ExtractIso(string isoFilePath, string extractionDirectory) {
        await Task.Run(() => {
            FileStream stream = File.OpenRead(isoFilePath);
            CDReader reader = new(stream, true);
            List<Task> taskList = [];
            ExtractDirectory("");
            Task.WhenAll(taskList).Wait();
            reader.Dispose();
            stream.Dispose();

            return;

            void ExtractDirectory(string pathInIso) {
                foreach (string directory in reader.GetDirectories(pathInIso)) {
                    string extractDir = Path.Join(extractionDirectory, directory.Replace("\\", "/"));
                    Console.WriteLine(/*directory + "\n" + #1#extractDir);
                    Directory.CreateDirectory(extractDir);
                    ExtractDirectory(directory);
                }


                foreach (var file in reader.GetFiles(pathInIso)) {
                    string extractFilePath = Path.Join(extractionDirectory, file.Replace("\\", "/"));
                    Console.WriteLine(/*file + "\n" +#1# extractFilePath);
                    taskList.Add(ExtractFile(file, extractFilePath));
                }
            }

            async Task ExtractFile(string file, string extractFile) {
                Stream isoStream = reader.OpenFile(file, FileMode.Open);
                Stream outputFile = File.Create(extractFile);
                await isoStream.CopyToAsync(outputFile);
                await isoStream.DisposeAsync();
                await outputFile.DisposeAsync();
            }
        });
    }*/

    /*public static async Task ExtractIso(string isoFilePath, string extractionDirectory) {
        await Task.Run(() => {
            using FileStream stream = File.OpenRead(isoFilePath);
            CDReader reader = new(stream, true);
            ExtractDirectory("");
            reader.Dispose();
            return;
            void ExtractDirectory(string pathInIso) {
                foreach (string directory in reader.GetDirectories(pathInIso)) {
                    string extractDir = Path.Join(extractionDirectory, directory.Replace("\\", "/"));
                    Console.WriteLine(/*directory + "\n" + #1#extractDir);
                    Directory.CreateDirectory(extractDir);
                    ExtractDirectory(directory);
                }


                foreach (var file in reader.GetFiles(pathInIso)) {
                    string extractFile = Path.Join(extractionDirectory, file.Replace("\\", "/"));
                    Console.WriteLine(/*file + "\n" +#1# extractFile);

                    Stream isoStream = reader.OpenFile(file, FileMode.Open);
                    FileStream outputFile = File.Create(extractFile);
                    isoStream.CopyTo(outputFile);
                    isoStream.Dispose();
                    outputFile.Dispose();
                }
            }
        });
    }*/

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

    /*public static void RepackIso(string sourceDirectoryPath, string outputIsoPath, Action<float>? progressCallback = null) {
        CDBuilder cdBuilder = new (){ UseJoliet = false, VolumeIdentifier = "PSP_GAME" };

        foreach (string filePath in Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.AllDirectories))
            cdBuilder.AddFile(Path.GetRelativePath(sourceDirectoryPath, filePath).Replace('/', '\\'), filePath);

        using var isoStream = File.Create(outputIsoPath);
        cdBuilder.Build(isoStream);
    }*/
    /*public static void RepackIso(string sourceDirectoryPath, string outputIsoPath, Action<float>? progressCallback = null) {
        CDBuilder cdBuilder = new() { UseJoliet = true, VolumeIdentifier = "PSP_GAME" };

        string[] files = Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.AllDirectories);
        int processedFiles = 0;

        foreach (string filePath in files) {
            string relativePath = Path.GetRelativePath(sourceDirectoryPath, filePath)
                .Replace('/', '\\');

            Console.WriteLine(relativePath);
            cdBuilder.AddFile(relativePath, filePath);

            processedFiles++;
            progressCallback?.Invoke((float)processedFiles / files.Length);
        }

        using var isoStream = File.Create(outputIsoPath);
        cdBuilder.Build(isoStream);
    }*/

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
