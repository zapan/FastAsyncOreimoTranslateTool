using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NanoXLSX;
using OBJEditor;
using RyuujiApi;

namespace TranslateCLI;

public class TranslationProjectCli {
    const byte vTORADORA = 0, vOREIMO = 1;
    byte Version = vTORADORA;

    public string? CurrentFile { get; private set; }
    public string BasePath { get; }
    public string MainFilePath { get; }
    public List<TranslationRow> Strings { get; } = new();
    public List<FileProgress> Files { get; } = new();

    public TranslationProjectCli(string? basePath = null) {
        BasePath = basePath ?? AppContext.BaseDirectory;
        MainFilePath = Path.Combine(BasePath, "Data", "Translation.json");
        EnsureDataFiles();
        DetectVersion();
        LoadFileList();
    }

    private void DetectVersion() {
        string gameName = IsoTools.DetectGameFromIso(Path.Combine(BasePath, "Data", "Iso"));
        Version = gameName == "Toradora" ? vTORADORA : vOREIMO;
    }

    public void EnsureDataFiles() {
        Directory.CreateDirectory(Path.Combine(BasePath, "Data"));
        if (!File.Exists(MainFilePath))
            File.WriteAllText(MainFilePath, "{ }");
    }

    public void LoadFileList() {
        Files.Clear();

        List<string> directories = new();
        string txtRoot = Path.Combine(BasePath, "Data", "Txt");
        string objRoot = Path.Combine(BasePath, "Data", "Obj");

        if (Directory.Exists(txtRoot)) {
            foreach (string path in Directory.GetDirectories(txtRoot))
                directories.Add(Path.GetFileName(path)!);
        }

        if (Directory.Exists(objRoot)) {
            foreach (string path in Directory.GetDirectories(objRoot))
                directories.Add(Path.GetFileName(path)!);
        }

        JObject mainFile = JObject.Parse(File.ReadAllText(MainFilePath));

        foreach (string name in directories.Distinct().OrderBy(x => x)) {
            double translationPercent = 0;
            if (mainFile[name] != null) {
                int stringCount = mainFile[name]!.Children().Children().Count();
                int translatedCount = 0;
                for (int i = 0; i < stringCount; i++) {
                    if (mainFile[name]![i.ToString()]?.ToString() != "")
                        translatedCount++;
                }

                if (stringCount > 0)
                    translationPercent = Math.Round((double)(translatedCount * 100) / stringCount, 1);
            }

            Files.Add(new FileProgress {
                FileName = name,
                TranslationPercent = translationPercent
            });
        }
    }

    public void LoadFile(string filename) {
        if (CurrentFile != null)
            SaveProgress();

        Console.WriteLine($"LoadFile {filename}");

        CurrentFile = filename;
        string[] myStrings;
        Dictionary<int, string?> myNames = new();

        if (Path.GetExtension(CurrentFile).Equals(".obj", StringComparison.OrdinalIgnoreCase)) {
            string filepath = Path.Combine(BasePath, "Data", "Obj", CurrentFile, CurrentFile);
            ObjHelper myHelper = new(File.ReadAllBytes(filepath), Version);
            myStrings = myHelper.Import();
            myNames = myHelper.Actors ?? new Dictionary<int, string?>();
        } else {
            string filepath = Path.Combine(BasePath, "Data", "Txt", CurrentFile, CurrentFile);
            myStrings = File.ReadAllLines(filepath, new UnicodeEncoding(false, false));
        }

        JObject mainFile = JObject.Parse(File.ReadAllText(MainFilePath));
        bool haveTranslation = mainFile[CurrentFile] != null;

        Strings.Clear();
        for (int i = 0; i < myStrings.Length; i++) {
            string name = "";
            string sentence;
            string translated = "";

            if (myStrings[i].StartsWith("「") && myStrings[i].EndsWith("」")) {
                if (myNames.TryGetValue(i, out string? actorName))
                    name = actorName ?? "";

                sentence = myStrings[i].TrimStart('「').TrimEnd('」');
            } else {
                sentence = myStrings[i];
            }

            sentence = sentence.Replace("＿", " ");

            if (haveTranslation && mainFile[CurrentFile]?[i.ToString()] is { } translationToken) {
                translated = translationToken.ToString();

                if (translated.StartsWith('「') && translated.EndsWith('」'))
                    translated = translated.TrimStart('「').TrimEnd('」');

                if (translated.StartsWith('（') && translated.EndsWith('）'))
                    translated = translated.TrimStart('（').TrimEnd('）');
            }

            Strings.Add(new TranslationRow {
                Name = name,
                Sentence = sentence,
                Translated = translated
            });
        }
    }

    public void SaveProgress() {
        if (CurrentFile == null)
            return;

        JObject mainFile = JObject.Parse(File.ReadAllText(MainFilePath));
        int translatedCount = 0;

        if (mainFile[CurrentFile] != null) {
            for (int i = 0; i < Strings.Count; i++) {
                string translatedString = Strings[i].Translated ?? "";
                if (!string.IsNullOrEmpty(translatedString)) {
                    translatedCount++;

                    if (Strings[i].Sentence.StartsWith("（") && Strings[i].Sentence.EndsWith("）"))
                        translatedString = "（" + translatedString + "）";

                    if (!string.IsNullOrEmpty(Strings[i].Name))
                        translatedString = "「" + translatedString + "」";
                }

                mainFile[CurrentFile]![i.ToString()] = translatedString;
            }
        } else {
            JObject translatedStrings = new();
            for (int i = 0; i < Strings.Count; i++) {
                string translatedString = Strings[i].Translated ?? "";
                if (!string.IsNullOrEmpty(translatedString)) {
                    translatedCount++;
                    if (!string.IsNullOrEmpty(Strings[i].Name))
                        translatedString = "「" + translatedString + "」";
                }

                translatedStrings.Add(i.ToString(), translatedString);
            }

            mainFile.Add(new JProperty(CurrentFile, translatedStrings));
        }

        File.WriteAllText(MainFilePath, mainFile.ToString());
        UpdateFilePercent(CurrentFile,
            Strings.Count == 0 ? 0 : Math.Round((translatedCount * 100.0) / Strings.Count, 1));
    }

    public void ExportText(string filename) {
        Workbook workbook = new(filename, "Sheet1");
        foreach (var row in Strings) {
            workbook.CurrentWorksheet.AddNextCell(row.Name);
            workbook.CurrentWorksheet.AddNextCell(row.Sentence);
            workbook.CurrentWorksheet.AddNextCell(row.Translated);
            workbook.CurrentWorksheet.GoToNextRow();
        }

        workbook.Save();
    }

    public void ExportAll(string folderPath) {
        Directory.CreateDirectory(folderPath);
        foreach (var file in Files) {
            LoadFile(file.FileName);
            ExportText(Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file.FileName) + ".xlsx"));
        }
    }

    private string TextReplacer(string input) {
        string result = input;
        result = result.Replace("[", "【");
        result = result.Replace("]", "】");
        result = result.Replace("(", "（");
        result = result.Replace(")", "）");
        result = result.Replace("{", "｛");
        result = result.Replace("}", "｝");
        result = result.Replace(":", "：");
        if (!(result.Contains('｛') && result.Contains('｝') && result.Contains('：'))) {
            result = result.Replace("：", ":");
        }

        return result;
    }

    public void ImportText(string filename, int column, int cell) {
        Workbook myWorkbook = Workbook.Load(filename);
        for (int i = cell; i <= Strings.Count; i++) {
            string cellKey = GetColumnName(column - 1) + i.ToString();
            try {
                string translated = TextReplacer(myWorkbook.CurrentWorksheet.Cells[cellKey].Value?.ToString() ?? "");
                Strings[i - 1].Translated = translated;
            } catch (KeyNotFoundException ex) when (ex.HResult == -2146232969) {
                Strings[i - 1].Translated = "";
            }
        }
    }

    public void ImportAll(string folderPath, int column, int cell) {
        foreach (var file in Files) {
            string xlsxFilename = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file.FileName) + ".xlsx");
            if (!File.Exists(xlsxFilename))
                continue;

            LoadFile(file.FileName);
            ImportText(xlsxFilename, column, cell);
            SaveProgress();
        }
    }

    public List<string> GetAllNames() {
        List<string> uniqueNames = new();

        foreach (var file in Files) {
            if (Path.GetExtension(file.FileName) != ".obj")
                continue;

            string filepath = Path.Combine(BasePath, "Data", "Obj", file.FileName, file.FileName);
            ObjHelper myHelper = new(File.ReadAllBytes(filepath), Version);
            myHelper.Import();
            Dictionary<int, string?> myNames = myHelper.Actors ?? new Dictionary<int, string?>();

            for (int i = 0; i < myNames.Count; i++) {
                if (myNames.TryGetValue(i, out string? value) && value != null && !uniqueNames.Contains(value))
                    uniqueNames.Add(value);
            }
        }

        return uniqueNames;
    }

    public void InsertLineBreaks(LineBreaksInserterCLI inserter) {
        for (int i = 0; i < Strings.Count; i++) {
            string currentString = Strings[i].Translated ?? "";
            bool isSpeech = !string.IsNullOrEmpty(Strings[i].Name);
            string newString = inserter.InsertLineBreaks(currentString, isSpeech);
            string[] parts = newString.Split('＿');
            if (parts.Length >= 4) {
                int keySeparator = (parts.Length == 4) ? 2 : 3;
                currentString = string.Join('＿', parts[..keySeparator]) + "[" +
                                string.Join('＿', parts[keySeparator..]) + "]";
                currentString = currentString.Replace("＿", " ");
                newString = inserter.InsertLineBreaks(currentString, isSpeech);
            }

            Strings[i].Translated = newString;
        }
    }

    private static string ScaleFontSizeString(LineBreaksInserterCLI inserter, string currentString, bool isSpeech,
        int fontSizePct) {
        if (currentString.Contains('｛') && currentString.Contains('｝') && currentString.Contains('：')) {
            currentString = RemoveSizeControl(currentString);
        }

        currentString = currentString.Replace("＿", " ");
        inserter.UpdateFontSizeScale(fontSizePct - 15);
        string newString = inserter.InsertLineBreaks(currentString, isSpeech);
        inserter.UpdateFontSizeScale(100);
        newString = "｛" + fontSizePct.ToString() + "：" + newString + "｝";
        return newString;
    }

    public static string RemoveSizeControl(string textoOriginal) {
        string pattern = @"｛\d{1,2}：|｝";
        string newString = Regex.Replace(textoOriginal, pattern, "");
        // Console.WriteLine(newString);
        return newString;
    }

    public void InsertLineBreaksAll(string dumpedFontFile, int maxWidth = 455) {
        LineBreaksInserterCLI inserter = new(dumpedFontFile, maxWidth);
        foreach (var file in Files) {
            LoadFile(file.FileName);
            InsertLineBreaks(inserter);
            SaveProgress();
        }
    }

    public void RemoveLineBreaks() {
        for (int i = 0; i < Strings.Count; i++)
            Strings[i].Translated = (Strings[i].Translated ?? "").Replace('＿', ' ');
    }

    public void RemoveLineBreaksAll() {
        foreach (var file in Files) {
            LoadFile(file.FileName);
            RemoveLineBreaks();
            SaveProgress();
        }
    }

    public double GetTotalPercent() {
        if (Files.Count == 0)
            return 0;

        double currentPercent = 0;
        foreach (var file in Files)
            currentPercent += file.TranslationPercent;

        return Math.Round(currentPercent / Files.Count, 1);
    }

    public void PrintCurrentStrings(TextWriter writer, int take = int.MaxValue) {
        int count = Math.Min(take, Strings.Count);
        for (int i = 0; i < count; i++) {
            writer.WriteLine($"{i + 1:0000} | {Strings[i].Name} | {Strings[i].Sentence} | {Strings[i].Translated}");
        }
    }

    private void UpdateFilePercent(string fileName, double percent) {
        FileProgress? row = Files.FirstOrDefault(f => f.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (row != null)
            row.TranslationPercent = percent;
    }

    private static string GetColumnName(int index) {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string value = "";

        if (index >= letters.Length)
            value += letters[index / letters.Length - 1];

        value += letters[index % letters.Length];
        return value;
    }
}