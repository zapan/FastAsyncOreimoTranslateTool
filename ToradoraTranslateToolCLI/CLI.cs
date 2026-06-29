using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NativeFileDialogSharp;
using OBJEditor;
using Newtonsoft.Json.Linq;

namespace ToradoraTranslateToolCLI;

public static class Cli {
    public static string StartupPath { get; private set; } = Directory.GetCurrentDirectory();
    public static string DataDir = Path.Combine(StartupPath, "Data");
    public static string mainFilePath = Path.Combine(StartupPath, "Data", "Translation.json");
    public static RyuujiApi.RyuujiApi Api = new(StartupPath);
    public static TranslateCLI.TranslationProjectCli TranslationApp = new(StartupPath);

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
            Console.WriteLine($"StartupPath: {StartupPath}");
            Console.WriteLine(
                """
                1. Extract iso
                2. Extract dat
                3. Repack dat
                4. Start game
                5. Save iso
                6. Exit app
                7. Parse Objs
                8. Export Translations
                9. Import Translations

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

            case '7':
                LoadFile("/Users/zapan/RandomProjects/Oreimo/FastAsyncToradoraTranslateTool/Data/Obj/_0000ESS1.obj/_0000ESS1.obj");
                LoadFile("/Users/zapan/RandomProjects/Oreimo/FastAsyncToradoraTranslateTool/Data/Obj/000scriptAKYO_0000A.obj/000scriptAKYO_0000A.obj");
                LoadFile("/Users/zapan/RandomProjects/Oreimo/FastAsyncToradoraTranslateTool/Data/Obj/000scriptMGIM_0000.obj/000scriptMGIM_0000.obj");
                break;

            case '8':
                TranslationApp.ExportAll(Path.Combine(DataDir, "Xlsx"));
                TranslationApp.SaveProgress();
                break;

             case '9':
                TranslationApp.ImportAll(Path.Combine(DataDir, "Xlsx"), 3, 1);
                break;

            case '6': // Exit app
                throw new("Goodbye!");

            default:
                return false;
        }
        
        Console.WriteLine($"Completed in {meow.ElapsedMilliseconds} ms");
        return true;
    }


    static void LoadFile(string filename)
    {
        Console.WriteLine();
        Console.WriteLine($"LoadFile {filename}");

//         if (currentFile != null)
//             SaveProgress();

        string currentFile = filename;
        string[] myStrings;
        Dictionary<int, string> myNames = new();
        if (Path.GetExtension(currentFile) == ".obj")
        {
            string filepath = Path.Combine(StartupPath, "Data", "Obj", currentFile, currentFile);
            ObjHelper myHelper = new(File.ReadAllBytes(filepath));
            myStrings = myHelper.Import();
            myNames = myHelper.Actors;
        }
        else // Else it is .txt file
        {
            string filepath = Path.Combine(StartupPath, "Data", "Txt", currentFile, currentFile);
            myStrings = File.ReadAllLines(filepath, new UnicodeEncoding(false, false)); // Txt file has encoding UTF-16 LE (Unicode without BOM)
        }

        JObject mainFile = JObject.Parse(File.ReadAllText(mainFilePath));
        bool haveTranslation = mainFile[currentFile] != null;

//         dataGridViewStrings.Rows.Clear();
        for (int i = 0; i < myStrings.Length; i++)
        {
            string name = "";
            string sentence;
            string translated = "";
            if (myStrings[i].StartsWith("「") && myStrings[i].EndsWith("」"))
            {
                name = myNames[i];
                sentence = myStrings[i].TrimStart('「').TrimEnd('」'); // Remove brackets from the beginning and end of the original sentence
            }
            else
                sentence = myStrings[i];
            sentence = sentence.Replace("＿", " ");


            if (haveTranslation && mainFile[currentFile][i.ToString()] is { } translationToken) {
                translated = translationToken.ToString();

                if (translated.StartsWith('「') && translated.EndsWith('」'))
                    translated = translated.TrimStart('「').TrimEnd('」'); // Remove brackets from the beginning and end of the original sentence

                if (translated.StartsWith('（') && translated.EndsWith('）'))
                    translated = translated.TrimStart('（').TrimEnd('）'); // Remove brackets from the beginning and end of the original sentence
            }

            Console.WriteLine($"[{i}] {name} : {sentence} : {translated}");
//             dataGridViewStrings.Rows.Add(name, sentence, translated);
        }
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
