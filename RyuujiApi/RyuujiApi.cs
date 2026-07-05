using System.Data;
using System.Diagnostics;

namespace RyuujiApi;

public class RyuujiApi(string startUpPath) {
    public Task ExtractIso(string isoPath, Action<string>? fileCallback = null, Action<byte>? progressCallback = null) =>
        IsoTools.ExtractIso(isoPath, Path.Combine(startUpPath, "Data", "Iso"), fileCallback, progressCallback);    
    /*IsoTools.ExtractIso(Path.Combine(_startupPath, "Data", "Iso"), isoPath, progressCallback);*/

    public void RepackIso(string mkisofs, string isoDirectory, string outputPath, Action<float>? progressCallback = null) =>
        IsoTools.RepackIso(mkisofs, isoDirectory, outputPath, progressCallback);
        /*IsoTools.RepackIso(Path.Combine(startUpPath, "Resources", "!!Tools", "Mkisofs"), isoDirectory, outputPath, progressCallback);*/

    public string DetectGameFromIso(string isoPath) {
        return IsoTools.DetectGameFromIso(isoPath);
    }

    public Task ExtractGame(string dataDir) =>
        Task.WhenAll(
             Task.Run(() => { // resource
                string extractionPath = Path.Combine(dataDir, "Iso", "PSP_GAME", "INSDIR", "RES.DAT");
                if (File.Exists(extractionPath)) {
                    DatTools.ExtractDat(startUpPath, extractionPath).Wait();
                    ObjTools.ProcessObjGz(startUpPath, Path.Combine(dataDir, "Extracted", "RES")).Wait();
                }
                if (File.Exists(Path.Combine(dataDir, "Iso", "PSP_GAME", "USRDIR", "resource.dat"))) {
                    DatTools.ExtractDat(startUpPath, Path.Combine(dataDir, "Iso", "PSP_GAME", "USRDIR", "resource.dat")).Wait();
                    ObjTools.ProcessObjGz(startUpPath, Path.Combine( dataDir, "Extracted", "resource")).Wait();
                }
             }),
             Task.Run(() => { // first
                 DatTools.ExtractDat(startUpPath, Path.Combine(dataDir, "Iso", "PSP_GAME", "USRDIR", "first.dat")).Wait();
                 if (File.Exists(Path.Combine(dataDir, "Extracted", "first", "text", "utf16.txt.gz"))) {
                     ObjTools.ProcessTxtGz(startUpPath, Path.Combine(dataDir, "Extracted", "first")).Wait();
                 }

                 if (File.Exists(Path.Combine(dataDir, "Extracted", "first", "seekmap", "res.map.gz"))) {
                     ObjTools.ProcessSeekmap(startUpPath, Path.Combine(dataDir, "Extracted", "first", "seekmap", "res.map.gz")).Wait();
                 } else {
                     ObjTools.ProcessSeekmap(startUpPath, Path.Combine(dataDir, "Extracted", "first", "seekmap.dat")).Wait();
                 }
             })
        );

    public async Task RepackGame(string dataDir, bool debugMode = false) {
        string resourceFile = "RES.DAT";
        string resourceFolder = "INSDIR";
        string extractionPath = Path.Combine(dataDir, "Iso", "PSP_GAME", resourceFolder, resourceFile);
        if (!File.Exists(extractionPath)) {
            resourceFile = "resource.dat";
            resourceFolder = "USRDIR";
        }

        await Task.Run(() => ObjTools.RepackObjs(startUpPath, debugMode));
        await Task.Run(() => ObjTools.RepackTxts(startUpPath));
        await Task.Run(() => DatTools.RepackDat(startUpPath, Path.Combine(dataDir, "Extracted", resourceFile + "-LstOrder.lst")));
        await Task.Run(() => ObjTools.RepackSeekmap(startUpPath, Path.Combine(dataDir, "Extracted", resourceFile), Path.Combine(dataDir, "Extracted", "first")));
        if (File.Exists(Path.Combine(dataDir, "Extracted", "first", "seekmap", "res.map.gz"))) {
            File.Move(Path.Combine(dataDir, "Extracted", "first", "seekmap.dat"), Path.Combine(dataDir, "Extracted", "first", "seekmap", "res.map.gz"), true);
        }
        await Task.Run(() => DatTools.RepackDat(startUpPath, Path.Combine(dataDir, "Extracted", "first.dat-LstOrder.lst")));
        await File.Create(Path.Combine(dataDir, "Extracted", "-")).DisposeAsync();
        await Task.Run(() => File.Copy(Path.Combine(dataDir, "Extracted", resourceFile), Path.Combine(dataDir, "Iso", "PSP_GAME", resourceFolder, resourceFile), true));
        await Task.Run(() => File.Copy(Path.Combine(dataDir, "Extracted", "first.dat"), Path.Combine(dataDir, "Iso", "PSP_GAME", "USRDIR", "first.dat"), true));
    }

    public async Task StartGame() {
        string filename = Path.Join(startUpPath, "Resources", "StartGame.conf");
        
        if (!File.Exists(filename) || new FileInfo(filename).Length == 0) {
            File.Create(filename);
            string errorText = $"Not configured!\r\n{Path.GetFullPath(filename)}\r\n{StartGameHelpText}";
            throw new DataException(errorText);
        }

        string[] fileContents = await File.ReadAllLinesAsync(filename);
        using Process process = new();
            
        string args = "";
        if (fileContents.Length == 2)
            args = fileContents[1];
            
        process.StartInfo = new()
        {
            FileName = fileContents[0],
            Arguments = args + " " + Path.Combine(startUpPath, "Data", "Iso")
        };
        Console.WriteLine(process.StartInfo.FileName + " " + process.StartInfo.Arguments);
        process.Start();
        await process.WaitForExitAsync();
    }
    
    public const string StartGameHelpText = 
        "This isn't necessarily a stage\r\n" +
        "Just a utility button that helps you test your changes after packing the game\r\n" +
        "You can configure this process with the StartGame.conf\r\n" + 
        "Put the path of the executable on the first line and the args on the second\r\n" +
        "The path of the Iso folder will be appended to the arguments\r\n" +
        "Example:\r\n" +
        "{executable}\r\n" +
        "{arguments}";
}
