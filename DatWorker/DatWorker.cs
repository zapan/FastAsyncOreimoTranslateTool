using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CppPorts;
using Newtonsoft.Json;
using tenoriTool;

namespace DatWorker;

public class DatWorker(string workingDir) {
    private readonly JsonSerializerSettings settings = new() {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
    };
    
    public async Task Process(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine("### Fast Async Dat Worker ####");
            Console.WriteLine("Toradora DAT Automation Tool");
            Console.WriteLine("Drop Dat files");
            return;
        }

        foreach (string arg in args) {
            if (arg.ToLower().EndsWith("-LstOrder.lst".ToLower())) { // Repack
                string lstOrder = await File.ReadAllTextAsync(arg);
                DatTree repackDatTree = JsonConvert.DeserializeObject<DatTree>(lstOrder);
                await SaveDat(workingDir, repackDatTree);
                Console.WriteLine($"Done repacking {arg}");
                continue;
            }
            
            // unpack
            DatTree datTree = new(GetDatLstDir(arg)[workingDir.Length..]);
            await OpenDat(arg, datTree);
            await File.WriteAllTextAsync(arg + "-LstOrder.lst", JsonConvert.SerializeObject(datTree, settings));
            Console.WriteLine($"Done unpacking {arg}");
        }
    }
    
    public class DatTree(string name, bool wasNoExtension = false) {
        [JsonProperty("n")] public string Name = name;
        [JsonProperty("c")] public List<DatTree> Children;
        [JsonProperty("e")] public bool WasNoExtension = wasNoExtension; // True if file originally had no extension
    }
    
    
    #region EXTRACT

    private async Task<DatTree> OpenDat(string dat, DatTree parent) {
        try {
            _ = Console.Out.WriteLineAsync($"Extracting: {dat}");
            var files = await ExtractDatContent(dat);
            if (files == null) return new DatTree(null);
            List<Task<DatTree>> taskList = [];
            foreach (string file in files) {
                string ext = Path.GetExtension(file).ToLower().Trim(' ', '.');
                // For Oreimo: check for .dat extension OR no extension (we added .dat temporarily)
                bool wasNoExtension = ext != "dat" && string.IsNullOrEmpty(Path.GetExtension(file).Trim(' ', '.'));
                if (ext == "dat" || wasNoExtension) {
                    string originalName = GetDatLstDir(file)[workingDir.Length..];
                    taskList.Add(OpenDat(file, new DatTree(originalName, wasNoExtension)));
                }
            }

            await Task.WhenAll(taskList);

            if (taskList.Count == 0) return parent;
            
            parent.Children ??= [];
            foreach (Task<DatTree> task in taskList)
                if (task.Result.Name != null) 
                    parent.Children.Add(task.Result);
            
            return parent;
        }
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<string[]> ExtractDatContent(string dat) {
        string newDir = Path.Combine(workingDir, Path.GetDirectoryName(dat)!, Path.GetFileNameWithoutExtension(dat));
        if (newDir.StartsWith('\\'))
            newDir = "." + newDir;

        TenoriToolApi.TenoriCallbacks callbacks = TenoriToolApi.TenoriCallbacks.None();
        TenoriToolApi.ProcessReturn processReturn = await TenoriToolApi.ProcessIndividualExtract("", newDir, false, true, "", dat, callbacks);
        if (!processReturn.Success) return null;
        File.Delete(dat);

        string lst = GetDatLstDir(dat);
        if (lst.StartsWith('\\'))
            lst = "." + lst;

        if (File.Exists(lst)) File.Delete(lst);
        await File.WriteAllTextAsync(lst, processReturn.MakeGpdaFileContent);

        // Get all files and add .dat extension to files without extension (for Oreimo)
        string[] allFiles = Directory.GetFiles(newDir, "*", SearchOption.AllDirectories);
        foreach (string file in allFiles) {
            if (string.IsNullOrEmpty(Path.GetExtension(file))) {
                File.Copy(file, file + ".dat");
                File.Move(file, file + ".dat2");
            }
        }

        return Directory.GetFiles(newDir, "*", SearchOption.AllDirectories);
    }

    private static string GetDatLstDir(string file) => Path.GetDirectoryName(file) + '/' + Path.GetFileNameWithoutExtension(file) + ".lst";

    #endregion

    #region REPACK
    private static async Task<bool> SaveDat(string workingDir, DatTree datTree) {
        if (datTree.Children != null) {
            List<Task<bool>> tasks = [];
            foreach (DatTree tree in datTree.Children)
                tasks.Add(SaveDat(workingDir, tree));
            await Task.WhenAll(tasks);
        }

        string realpath = Path.Join(workingDir, datTree.Name);
        if (!File.Exists(realpath)) return false;

        // For Oreimo files that originally had no extension, rename them back
        if (datTree.WasNoExtension && realpath.EndsWith(".dat")) {
            string originalPath = realpath.Substring(0, realpath.Length - 4); // Remove .dat
            File.Move(realpath, originalPath, true);
            realpath = originalPath;
        }

        _ = Console.Out.WriteLineAsync($"Repacking: {realpath}");
        await RepackDat(workingDir, realpath);
        return true;
    }

    private static async Task<bool> RepackDat(string workingDir, string lst) {
        try {
            string lstDir = $"{Path.GetDirectoryName(lst)}/";
            if (lstDir.StartsWith("\\"))
                lstDir = '.' + lstDir;

            if (lstDir.StartsWith(".\\"))
                lstDir = Path.Combine(workingDir, lstDir.Substring(2, lstDir.Length - 2));

            string file = lstDir + Path.GetFileNameWithoutExtension(lst);
            
            // Use platform-specific MakeGpda
#if __UNIX__
            await MakeGpdaMacOS.Process(file, lstDir);
#else
            await MakeGpda.Process(file, lstDir);
#endif
            return true;
        }
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion
}
