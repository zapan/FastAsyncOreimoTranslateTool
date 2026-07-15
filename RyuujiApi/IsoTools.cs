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

    public static async Task ExtractIso(string isoFilePath, string extractionDirectory,
        Action<string>? fileCallback = null, Action<byte>? progressCallback = null) {
        await using FileStream stream = new(isoFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024,
            FileOptions.SequentialScan);
        using CDReader reader = new(stream, true);

        long totalBytes = CalculateTotalSize("");
        long writtenBytes = 0;

        ExtractDirectory("");

        return;

        long CalculateTotalSize(string pathInIso) {
            long size = 0;
            foreach (string directory in reader.GetDirectories(pathInIso))
                size += CalculateTotalSize(directory);
            foreach (string file in reader.GetFiles(pathInIso))
                size += reader.GetFileInfo(file).Length;
            return size;
        }

        void ExtractDirectory(string pathInIso) {
            foreach (string directory in reader.GetDirectories(pathInIso)) {
                string extractDir = Path.Join(extractionDirectory, directory.Replace('\\', '/'));
                //Console.WriteLine(extractDir);
                Directory.CreateDirectory(extractDir);
                ExtractDirectory(directory);
            }

            foreach (string file in reader.GetFiles(pathInIso)) {
                ExtractFile(file).GetAwaiter().GetResult();
            }
        }

        async Task ExtractFile(string file) {
            fileCallback?.Invoke(file);
            string extractFilePath = Path.Join(extractionDirectory, file.Replace('\\', '/'));
            //Console.WriteLine(extractFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(extractFilePath)!);

            await using Stream isoStream = reader.OpenFile(file, FileMode.Open);
            await using FileStream outputFile = new(extractFilePath, FileMode.Create, FileAccess.Write, FileShare.None,
                1024 * 1024, FileOptions.SequentialScan);

            byte[] buffer = new byte[1024 * 1024];
            int readCount;
            while ((readCount = await isoStream.ReadAsync(buffer)) > 0) {
                await outputFile.WriteAsync(buffer.AsMemory(0, readCount));
                writtenBytes += readCount;
                progressCallback?.Invoke((byte)(writtenBytes * 100 / totalBytes));
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

    public static void RepackIso(string mkisofs, string isoDirectory, string outputPath,
        Action<float>? progressCallback = null) {
        if (!Directory.Exists(isoDirectory)) {
            throw new DirectoryNotFoundException($"iso directory not found: {isoDirectory}");
        }

        var gameName = DetectGameFromIso(isoDirectory);
        string command = "-iso-level 4 -xa -A \"PSP GAME\" -V \"" + gameName + "\" -sysid \"PSP GAME\" -volset \"" +
                         gameName + "\" -p \"\" -publisher \"\" -o \"" + outputPath + "\" \"" + isoDirectory + "\"";
        using Process myProc = new();
        myProc.StartInfo.FileName = "mkisofs";
        myProc.StartInfo.Arguments = command;
        myProc.StartInfo.CreateNoWindow = true;
        /*myProc.StartInfo.WorkingDirectory = mkisofs;*/
        myProc.StartInfo.RedirectStandardError = progressCallback != null;
        myProc.StartInfo.UseShellExecute = false;


        Console.WriteLine("Repacking ISO with command: " + myProc.StartInfo.FileName + " " +
                          myProc.StartInfo.Arguments);

        if (progressCallback != null)
            myProc.ErrorDataReceived += (_, args) => {
                /*Console.WriteLine(args.Data);*/
                if (args.Data == null) return;
                if (float.TryParse(args.Data.Trim().Split(' ')[0].Trim('%'), out float result))
                    progressCallback(Math.Clamp(100, 0, result));
            };

        try {
            myProc.Start();
            if (progressCallback != null) myProc.BeginErrorReadLine();
            myProc.WaitForExit();
        } catch (Exception) {
            Console.WriteLine(
                "Chances are you dont have cdrtools installed, \n\nto rebuild the iso you need to provide the mkisofs path in mkisofs.conf in the root directory of the application.");
            throw;
        }
    }

    public static string DetectGameFromIso(string isoDirectory) {
        string game = "";
        try {
            FileStream inputResStream = File.OpenRead(Path.Combine(isoDirectory, "UMD_DATA.BIN"));
            StreamReader inputResReader = new StreamReader(inputResStream);
            string? line = inputResReader.ReadLine();
            inputResReader.Dispose();
            if (string.IsNullOrWhiteSpace(line))
                throw new EndOfStreamException();
            string[] parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0]) {
                case "ULJS-00490":
                case "ULJS-00492":
                case "ULJS-19086":
                case "NPJH-50568":
                case "ULJS-00358":
                    game = "OreimoDisc1";
                    break;

                case "ULJS-00491":
                case "ULJS-00493":
                case "ULJS-19087":
                case "NPJH-50569":
                    game = "OreimoDisc2";
                    break;

                default:
                    game = "Toradora";
                    break;
            }

            Console.WriteLine($"Detected {game} Iso.");
        } catch (Exception) {
            Console.WriteLine($"Unable to detect Iso game. Using {game} as default.");
        }

        return game;
    }
}