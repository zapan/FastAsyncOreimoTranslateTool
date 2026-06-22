using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NativeFileDialogSharp;

namespace ToradoraTranslateToolCLI;

public static class Cli {
    public static string StartupPath { get; private set; } = Directory.GetCurrentDirectory();
    public static string DataDir = Path.Combine(StartupPath, "Data");
    public static RyuujiApi.RyuujiApi Api = new(StartupPath);
    
    static void Main(string[] args) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        // Check platform and warn if not supported
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Console.WriteLine("This CLI is designed for macOS but detected Windows platform.");
        } else if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            Console.WriteLine($"Warning: This CLI was designed for macOS but detected {RuntimeInformation.OSDescription}");
        }
        
        while (true) {
            Console.Clear();
            Console.WriteLine(
                """
                1. Extract iso
                2. Extract dat
                3. Repack dat
                4. Start game
                5. Save iso
                6. Exit app
                
                Type the index of the action you want to do
                """);

            while (true)
                if (ProcessInput(Console.ReadKey(true).KeyChar)) {
                    Console.ReadLine(); 
                    break;
                }
        }
    }

    static bool ProcessInput(char input) {
        Stopwatch meow = new ();
        switch (input) {
            case '1': // Extract iso
                string? path = OpenFilePicker("iso");
                if (path == null) break;
                
                meow.Start();
                string currentFile = "";
                Api.ExtractIso(path, file => {
                    currentFile = file;
                    Console.WriteLine(); 
                }, b => Console.Write($"\r[{b}]\t{currentFile}")).Wait();
                Console.WriteLine();
                break;
            
            case '2': // Extract dat
                meow.Start();
                Api.ExtractGame(DataDir).Wait();
                break;
            
            case '3': // Repack dat
                Console.WriteLine("This feature is not completed, issues may occur");
                Console.WriteLine("Debug mode? Y\\N");
                bool debug = Console.ReadKey(true).KeyChar.ToString().ToLowerInvariant() is "y";
                meow.Start();
                Api.RepackGame(DataDir, debug).Wait();
                break;
            
            case '4': // Start game
                meow.Start();
                Api.StartGame().Wait();
                break;
            
            case '5': // Save iso
                string? selectedPath = OpenFilePicker(save: true);
                if (selectedPath == null) break;
                
                meow.Start();
                string isoPath = Path.Combine(DataDir, "Iso");
                string mkisofs = "mkisofs";
                if (File.Exists("mkisofs.conf")) mkisofs = File.ReadAllText("mkisofs.conf");
                Api.RepackIso(mkisofs, isoPath, selectedPath);
                break;
            
            case '6': // Exit app
                throw new("Goodbye!");
            
            default:
                return false;
        }
        
        Console.WriteLine($"Completed in {meow.ElapsedMilliseconds} ms");
        return true;
    }

    static string? OpenFilePicker(string filterList = "", bool save = false) {
        bool canceled = false;
        string? path = null;
        while (true) {
            try {
                if(save){
                    string posix =  "POSIX path of (choose file name with prompt \"Guardar como\")";
                } else {
                    string posix = "POSIX path of (choose file with prompt \"Selecciona un archivo\")";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "osascript",
                    ArgumentList =
                    {
                        "-e",
                        "POSIX path of (choose file with prompt \"Selecciona un archivo\")"
                    },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using var p = Process.Start(psi)!;
                string stdout = p.StandardOutput.ReadToEnd().Trim();
                string stderr = p.StandardError.ReadToEnd().Trim();
                p.WaitForExit();

                if (p.ExitCode != 0)
                    throw new Exception(string.IsNullOrWhiteSpace(stderr) ? "Cancelado" : stderr);

                path = stdout;

                Console.WriteLine($"path is {path}");
            }
            catch (Exception e) {
                Console.WriteLine("Exception");
                Console.WriteLine("\n\nThere was a non-fatal error trying to start a file picker,\n"
#if _WINDOWS
                                    + "Make sure you have the c++ visual redistributable installed,\n"
#endif
#if __APPLE__
                                    + "Make sure you have the native dependencies installed (Homebrew: brew install libffi libplist),\n"
#endif
                                  + "Press Y to show the error, anything else to skip.");
                if (Console.ReadKey(true).KeyChar.ToString().ToLowerInvariant() is "y") Console.WriteLine(e);
                    
                Console.WriteLine($"Enter the path of {(/*save? "the folder" :*/ "the file")}, or type \"EXIT\" to cancel:");
                if ((path = Console.ReadLine()?.Trim('\"'))?.ToLower().Trim() == "exit") canceled = true;
            }
            
            if (canceled || save || File.Exists(path)) break;
            Console.WriteLine("File not found, try again");
        }
                
        if (canceled) path = null;
        return path;
    }
}
