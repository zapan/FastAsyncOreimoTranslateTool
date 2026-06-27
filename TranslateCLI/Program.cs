using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using OBJEditor;
using Newtonsoft.Json.Linq;
using System.Reflection;
using NanoXLSX;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NativeFileDialogSharp;
using OBJEditor;

namespace TranslateCLI;

public static class Program {

    public static string StartupPath { get; private set; } = Directory.GetCurrentDirectory();

    static void ShowUsage()
    {
        Console.WriteLine("ToradoraTranslateTool CLI");
        Console.WriteLine("Commands:");
        Console.WriteLine("  list-files [basePath]");
        Console.WriteLine("  load <fileName> [basePath]");
        Console.WriteLine("  export <fileName> <xlsxPath> [basePath]");
        Console.WriteLine("  export-all <folderPath> [basePath]");
        Console.WriteLine("  import <fileName> <xlsxPath> <column> <cell> [basePath]");
        Console.WriteLine("  import-all <folderPath> <column> <cell> [basePath]");
        Console.WriteLine("  linebreaks <fileName> <dumpedFontFile> [basePath]");
        Console.WriteLine("  linebreaks-all <dumpedFontFile> [basePath]");
        Console.WriteLine("  remove-linebreaks <fileName> [basePath]");
        Console.WriteLine("  remove-linebreaks-all [basePath]");
        Console.WriteLine("  names [basePath]");
    }

    static void Main(string[] args) {

        Console.WriteLine($"StartupPath: {StartupPath}");

        if (args.Length == 0)
        {
            ShowUsage();
            return;
        }

        try
        {
            string command = args[0].ToLowerInvariant();

            switch (command)
            {
                case "list-files":
                {
                    string? basePath = args.Length >= 2 ? args[1] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    Console.WriteLine($"Total: {app.GetTotalPercent():0.0}%");
                    foreach (var file in app.Files)
                        Console.WriteLine($"{file.FileName} | {file.TranslationPercent:0.0}%");
                    break;
                }

                case "load":
                {
                    if (args.Length < 2) throw new ArgumentException("Usage: load <fileName> [basePath]");
                    string? basePath = args.Length >= 3 ? args[2] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    app.LoadFile(args[1]);
                    app.PrintCurrentStrings(Console.Out, 50);
                    break;
                }

                case "export":
                {
                    if (args.Length < 3) throw new ArgumentException("Usage: export <fileName> <xlsxPath> [basePath]");
                    string? basePath = args.Length >= 4 ? args[3] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    app.LoadFile(args[1]);
                    app.ExportText(args[2]);
                    Console.WriteLine("Export completed.");
                    break;
                }

                case "export-all":
                {
                    if (args.Length < 2) throw new ArgumentException("Usage: export-all <folderPath> [basePath]");
                    string? basePath = args.Length >= 3 ? args[2] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    app.ExportAll(args[1]);
                    Console.WriteLine("Export all completed.");
                    break;
                }

                case "import":
                {
                    if (args.Length < 5) throw new ArgumentException("Usage: import <fileName> <xlsxPath> <column> <cell> [basePath]");
                    string? basePath = args.Length >= 6 ? args[5] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    app.LoadFile(args[1]);
                    app.ImportText(args[2], int.Parse(args[3]), int.Parse(args[4]));
                    app.SaveProgress();
                    Console.WriteLine("Import completed.");
                    break;
                }

                case "import-all":
                {
                    if (args.Length < 4) throw new ArgumentException("Usage: import-all <folderPath> <column> <cell> [basePath]");
                    string? basePath = args.Length >= 5 ? args[4] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    app.ImportAll(args[1], int.Parse(args[2]), int.Parse(args[3]));
                    Console.WriteLine("Import all completed.");
                    break;
                }

                case "linebreaks":
                {
                    if (args.Length < 3) throw new ArgumentException("Usage: linebreaks <fileName> <dumpedFontFile> [basePath]");
                    string? basePath = args.Length >= 4 ? args[3] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    app.LoadFile(args[1]);
                    var inserter = new LineBreaksInserterCLI(args[2], 455);
                    app.InsertLineBreaks(inserter);
                    app.SaveProgress();
                    Console.WriteLine("Line breaks inserted.");
                    break;
                }

                case "linebreaks-all":
                {
                    if (args.Length < 2) throw new ArgumentException("Usage: linebreaks-all <dumpedFontFile> [basePath]");
                    string? basePath = args.Length >= 3 ? args[2] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    app.InsertLineBreaksAll(args[1]);
                    Console.WriteLine("Line breaks inserted for all files.");
                    break;
                }

                case "remove-linebreaks":
                {
                    if (args.Length < 2) throw new ArgumentException("Usage: remove-linebreaks <fileName> [basePath]");
                    string? basePath = args.Length >= 3 ? args[2] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    app.LoadFile(args[1]);
                    app.RemoveLineBreaks();
                    app.SaveProgress();
                    Console.WriteLine("Line breaks removed.");
                    break;
                }

                case "remove-linebreaks-all":
                {
                    string? basePath = args.Length >= 2 ? args[1] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    app.RemoveLineBreaksAll();
                    Console.WriteLine("Line breaks removed for all files.");
                    break;
                }

                case "names":
                {
                    string? basePath = args.Length >= 2 ? args[1] : StartupPath;
                    var app = new TranslationProjectCli(basePath);
                    foreach (var name in app.GetAllNames())
                        Console.WriteLine(name);
                    break;
                }

                default:
                    ShowUsage();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            Environment.ExitCode = 1;
        }
    }




}
