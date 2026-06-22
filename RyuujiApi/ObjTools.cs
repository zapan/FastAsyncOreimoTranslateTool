using System.IO.Compression;
using System.Text;
using CppPorts;
using Newtonsoft.Json.Linq;
using OBJEditor;

namespace RyuujiApi;

class ObjTools {
    private static string MainFilePath(string sp) => Path.Combine(sp, "Data", "Translation.json");
    private static string TempDirectory(string sp) => Path.Combine(sp, "Data", "Temp");

    public static async Task ProcessObjGz(string startupPath, string directoryPath) {
        string objDir = Path.Combine(startupPath, "Data", "Obj");
        if (!Directory.Exists(objDir))
            Directory.CreateDirectory(objDir);

        string[] archives = Directory.GetFiles(directoryPath, "*.obj.gz", SearchOption.AllDirectories);

        List<Task> taskList = [];
        foreach (string archive in archives)
            taskList.Add(ProcessArchive(startupPath, archive, objDir));

        await Task.WhenAll(taskList);
    }

    public static async Task ProcessArchive(string startupPath, string archive, string objDir) {
        if (Path.GetFileName(archive) == "STARTPOINT.obj.gz") { // Save the original debug menu because it will be replaced by the one I translated
            File.Copy(archive, Path.Combine(startupPath, "Resources", "DebugMode", "original_STARTPOINT.obj.gz"), true);
            return;
        }

        string objFolder = Path.Combine(objDir, Path.GetFileNameWithoutExtension(archive));
        Directory.CreateDirectory(objFolder);

        string gzCopyPath = Path.Combine(objFolder, Path.GetFileName(archive));
        File.Copy(archive, gzCopyPath, true);

        string relTxtPath = Path.Combine(objFolder, Path.GetFileNameWithoutExtension(archive) + ".txt");
        await File.WriteAllTextAsync(relTxtPath, archive.Replace(startupPath, "")); // Write relative path to the original file in Data\Obj\%obj name%\%obj name%.txt

        string decompressedPath = Path.Combine(objFolder, Path.GetFileNameWithoutExtension(archive));
        await using FileStream input = File.OpenRead(gzCopyPath);
        await using FileStream output = File.Create(decompressedPath);
        await using GZipStream gzip = new(input, CompressionMode.Decompress);
        await gzip.CopyToAsync(output);
    }

    public static async Task ProcessTxtGz(string startupPath, string directoryPath) {
        string txtDir = Path.Combine(startupPath, "Data", "Txt");
        if (!Directory.Exists(txtDir))
            Directory.CreateDirectory(txtDir);

        string archive = Path.Combine(directoryPath, "text", "utf16.txt.gz");

        string txtFolder = Path.Combine(txtDir, Path.GetFileNameWithoutExtension(archive));
        Directory.CreateDirectory(txtFolder);

        string gzCopyPath = Path.Combine(txtFolder, Path.GetFileName(archive));
        File.Copy(archive, gzCopyPath, true);

        string relTxtPath = Path.Combine(txtFolder, Path.GetFileNameWithoutExtension(archive) + ".txt");
        await File.WriteAllTextAsync(relTxtPath, archive.Replace(startupPath, ""));

        string decompressedPath = Path.Combine(txtFolder, Path.GetFileNameWithoutExtension(archive));
        await using FileStream input = File.OpenRead(gzCopyPath);
        await using FileStream output = File.Create(decompressedPath);
        await using GZipStream gzip = new(input, CompressionMode.Decompress);
        await gzip.CopyToAsync(output);
    }

    public static async Task ProcessSeekmap(string startupPath, string firstDirectory) {
        string sourcePath = Path.Combine(firstDirectory, "seekmap.dat");
        Directory.CreateDirectory(TempDirectory(startupPath));
        string outPath = Path.Combine(TempDirectory(startupPath), "seekmap.txt");

        await using FileStream input = File.OpenRead(sourcePath);
        await using FileStream output = File.Create(outPath);
        await using GZipStream gzip = new(input, CompressionMode.Decompress);
        await gzip.CopyToAsync(output);
    }

    public static async Task RepackObjs(string startupPath, bool debugMode) {
        List<string> directories = [];
        foreach (string path in Directory.GetDirectories(Path.Combine(startupPath, "Data", "Obj")))
            directories.Add(Path.GetFileName(path));

        if (!File.Exists(MainFilePath(startupPath))) await File.WriteAllTextAsync(MainFilePath(startupPath), "{ }");
        JObject mainFile = JObject.Parse(await File.ReadAllTextAsync(MainFilePath(startupPath)));

        Dictionary<string, string> translatedNames = new(); // Dictionary with pairs of original and translated names
        if (mainFile["names"] != null)
            foreach (JProperty name in mainFile["names"].Children())
                if (name.Value.ToString() != "") // If a translation for that name exists
                    translatedNames.Add(name.Name, name.Value.ToString());

        List<Task> taskList = [];
        foreach (string name in directories)
            taskList.Add(RepackObj(startupPath, name, mainFile[name], translatedNames));

        await Task.WhenAll(taskList);

        if (debugMode) {
            File.Copy(Path.Combine(startupPath, "Resources", "DebugMode", "_0000ESS1.obj.gz"), Path.Combine(startupPath, "Data", "Extracted", "RES", "script", "_0000ESS1", "_0000ESS1.0001", "_0000ESS1.obj.gz"), true); // This file enables debug mode
            File.Copy(Path.Combine(startupPath, "Resources", "DebugMode", "STARTPOINT.obj.gz"), Path.Combine(startupPath, "Data", "Extracted", "RES", "script", "STARTPOINT", "STARTPOINT.0001", "STARTPOINT.obj.gz"), true); // This is pretranslated debug menu
        }
        else
            File.Copy(Path.Combine(startupPath, "Resources", "DebugMode", "original_STARTPOINT.obj.gz"), Path.Combine(startupPath, "Data", "Extracted", "RES", "script", "STARTPOINT", "STARTPOINT.0001", "STARTPOINT.obj.gz"), true); // Restore original debug menu
    }

    public static async Task RepackObj(string startupPath, string name, JToken translation, Dictionary<string, string> translatedNames) {
        string filepath = Path.Combine(startupPath, "Data", "Obj", name, name);
        ObjHelper myHelper = new(await File.ReadAllBytesAsync(filepath));
        string[] scriptStrings = myHelper.Import();
        Dictionary<int, string> scriptNames = myHelper.Actors;

        bool haveTranslation = translation != null;
        for (int i = 0; i < scriptStrings.Length; i++) {
            if (haveTranslation && translation[i.ToString()] is { } words) {
                string translatedString = words.ToString(); // not null bleeehhh
                if (translatedString != "")
                    scriptStrings[i] = translatedString;
            }

            if (scriptNames[i] != null && translatedNames.TryGetValue(scriptNames[i], out string value))
                scriptNames[i] = value;
        }

        byte[] data = myHelper.Export(scriptStrings);
        await using FileStream fileStream = File.Create(startupPath + await File.ReadAllTextAsync(filepath + ".txt"));
        await using GZipStream gzip = new(fileStream, CompressionLevel.Optimal);
        await gzip.WriteAsync(data);
    }

    public static async Task RepackTxts(string startupPath) {
        List<String> directories = [];
        foreach (string path in Directory.GetDirectories(Path.Combine(startupPath, "Data", "Txt")))
            directories.Add(Path.GetFileName(path));

        JObject mainFile = JObject.Parse(await File.ReadAllTextAsync(MainFilePath(startupPath)));

        List<Task> tasklist = [];
        foreach (string name in directories)
            if (mainFile[name] != null)
                tasklist.Add(RepackTxt(startupPath, name, mainFile[name])); // If json have translation for that file

        await Task.WhenAll(tasklist);
    }

    public static async Task RepackTxt(string startupPath, string name, JToken translation) {
        string filepath = Path.Combine(startupPath, "Data", "Txt", name, name);
        string[] fileLines = await File.ReadAllLinesAsync(filepath, new UnicodeEncoding(false, false));

        for (int i = 0; i < fileLines.Length; i++) {
            string translatedString = translation[i.ToString()]!.ToString(); // not nullll blehhhh
            if (!string.IsNullOrEmpty(translatedString))
                fileLines[i] = translatedString;
        }

        string destFile = startupPath + await File.ReadAllTextAsync(filepath + ".txt");
        byte[] data = new UnicodeEncoding(false, false).GetBytes(string.Join("\r\n", fileLines));
        await using FileStream file = File.Create(destFile);
        await using GZipStream gzip = new(file, CompressionLevel.Optimal);
        await gzip.WriteAsync(data);
    }

    public static async Task RepackSeekmap(string startupPath, string resourcePath, string firstDirectory) {
        string toolsDir = TempDirectory(startupPath);
        string resDir = Path.Combine(toolsDir, "RES.dat");
        File.Copy(resourcePath, resDir, true); // RES.dat and seekmap.txt required for modseekmap.exe
        /*using (Process myProc = new()) {
            myProc.StartInfo.FileName = Path.Combine(toolsDir, "modseekmap.exe"); // modseekmap generates seekmap.new file
            myProc.StartInfo.WorkingDirectory = toolsDir;
            myProc.StartInfo.CreateNoWindow = true;
            myProc.StartInfo.UseShellExecute = false;
            myProc.StartInfo.RedirectStandardError = true;
            myProc.StartInfo.RedirectStandardOutput = true;
            myProc.ErrorDataReceived += (_, args) => { if (args.Data != null) Console.WriteLine(args.Data); };
            myProc.OutputDataReceived += (_, args) => { if (args.Data != null) Console.WriteLine(args.Data); };
            
            myProc.Start();
            await myProc.WaitForExitAsync();
        }*/
        string oldName = Path.Combine(toolsDir, "seekmap.new");
        await ModSeekMap.ProcessAsync(resDir, Path.Join(toolsDir, "seekmap.txt"), oldName);

        // rename seekmap.new to seekmap.dat
        string newName = Path.Combine(toolsDir, "seekmap.dat");
        File.Move(oldName, newName, true);

        string inputFile = Path.Combine(toolsDir, "seekmap.dat");
        string outputFile = Path.Combine(Path.Combine(firstDirectory, "seekmap.dat"));

        if (File.Exists(outputFile)) File.Delete(outputFile);

        await using FileStream input = File.OpenRead(inputFile);
        await using FileStream output = File.Create(outputFile);
        await using GZipStream gzip = new(output, CompressionLevel.Optimal);
        await input.CopyToAsync(gzip);
    }
}