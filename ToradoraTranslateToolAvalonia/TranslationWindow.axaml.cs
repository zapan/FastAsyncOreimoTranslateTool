using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TranslateCLI;

namespace ToradoraTranslateToolAvalonia;

public partial class TranslationWindow : Window
{
    // ── Observable wrappers for Avalonia binding ─────────────────────────────

    /// <summary>Row shown in the file-list DataGrid.</summary>
    public class FileRow
    {
        public string FileName { get; set; } = "";
        public string ProgressText { get; set; } = "0%";
        internal double ProgressValue { get; set; } = 0;
    }

    /// <summary>Row shown in the strings DataGrid.</summary>
    public class StringRow : INotifyPropertyChanged
    {
        private string _translated = "";
        public string Name       { get; set; } = "";
        public string Sentence   { get; set; } = "";
        public string Translated
        {
            get => _translated;
            set { _translated = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Fields ────────────────────────────────────────────────────────────────

    private readonly TranslationProjectCli _project;
    private readonly ObservableCollection<FileRow> _fileRows = [];
    private readonly ObservableCollection<StringRow> _stringRows = [];
    private FileRow? _selectedFileRow;

    // ── Constructor ───────────────────────────────────────────────────────────

    public TranslationWindow()
    {
        InitializeComponent();

        _project = new TranslationProjectCli(AppContext.BaseDirectory);

        dataGridFiles.ItemsSource   = _fileRows;
        dataGridStrings.ItemsSource = _stringRows;

        LoadFileList();
        Closing += TranslationWindow_Closing;
    }

    // ── File list ─────────────────────────────────────────────────────────────

    private void LoadFileList()
    {
        _fileRows.Clear();

        // Total row at top
        _fileRows.Add(new FileRow { FileName = "Total:", ProgressText = "0%" });

        foreach (var fp in _project.Files)
        {
            _fileRows.Add(new FileRow
            {
                FileName      = fp.FileName,
                ProgressText  = fp.TranslationPercent + "%",
                ProgressValue = fp.TranslationPercent
            });
        }

        UpdateTotalPercent();
    }

    private void UpdateTotalPercent()
    {
        double total = _fileRows.Skip(1).Sum(r => r.ProgressValue);
        int count = _fileRows.Count - 1;
        double pct = count == 0 ? 0 : Math.Round(total / count, 1);
        _fileRows[0].ProgressText  = pct + "%";
        _fileRows[0].ProgressValue = pct;
    }

    private void UpdateRowPercent(string fileName, double percent)
    {
        var row = _fileRows.FirstOrDefault(r =>
            r.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (row == null) return;
        row.ProgressText  = percent + "%";
        row.ProgressValue = percent;
        UpdateTotalPercent();
        // Refresh the DataGrid cell
        int idx = _fileRows.IndexOf(row);
        if (idx >= 0)
        {
            _fileRows.RemoveAt(idx);
            _fileRows.Insert(idx, row);
        }
    }

    // ── Load a file into the strings grid ────────────────────────────────────

    private void LoadFile(FileRow fileRow)
    {
        // Save whatever was previously loaded
        if (_selectedFileRow != null)
            FlushEditsAndSave();

        _selectedFileRow = fileRow;
        _project.LoadFile(fileRow.FileName);

        _stringRows.Clear();
        foreach (var r in _project.Strings)
        {
            _stringRows.Add(new StringRow
            {
                Name       = r.Name,
                Sentence   = r.Sentence,
                Translated = r.Translated ?? ""
            });
        }
    }

    /// <summary>Push StringRow edits back into _project.Strings, then save.</summary>
    private void FlushEditsAndSave()
    {
        for (int i = 0; i < _stringRows.Count && i < _project.Strings.Count; i++)
            _project.Strings[i].Translated = _stringRows[i].Translated;

        _project.SaveProgress();

        if (_selectedFileRow != null)
        {
            var fp = _project.Files.FirstOrDefault(f =>
                f.FileName.Equals(_selectedFileRow.FileName, StringComparison.OrdinalIgnoreCase));
            if (fp != null)
                UpdateRowPercent(_selectedFileRow.FileName, fp.TranslationPercent);
        }
    }

    // ── DataGrid events ───────────────────────────────────────────────────────

    private void DataGridFiles_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (dataGridFiles.SelectedItem is FileRow row && row != _fileRows[0])
            LoadFile(row);
    }

    private void DataGridStrings_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        // Nothing extra needed; two-way binding handles it
    }

    // ── Context menu: strings grid ────────────────────────────────────────────

    private async void MenuLineBreaks_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedFileRow == null)
        {
            await ShowInfoAsync("First select the file!");
            return;
        }
        string? fontFile = await OpenFontFileAsync();
        if (fontFile == null) return;
        FlushEditsAndSave();
        _project.InsertLineBreaksAll(fontFile, 455);
        ReloadCurrentStrings();
    }

    private void MenuRemoveLineBreaks_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedFileRow == null) return;
        FlushEditsAndSave();
        _project.RemoveLineBreaks();
        ReloadCurrentStrings();
    }

    private async void MenuExportStrings_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedFileRow == null)
        {
            await ShowInfoAsync("First select the file!");
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export strings",
            DefaultExtension = "xlsx",
            FileTypeChoices = [new FilePickerFileType("Excel sheet") { Patterns = ["*.xlsx"] }],
            SuggestedFileName = Path.GetFileNameWithoutExtension(_selectedFileRow.FileName)
        });
        if (file == null) return;

        FlushEditsAndSave();
        _project.ExportText(file.Path.LocalPath);
    }

    private async void MenuImportStrings_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedFileRow == null)
        {
            await ShowInfoAsync("First select the file!");
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import strings",
            FileTypeFilter = [new FilePickerFileType("Excel sheet") { Patterns = ["*.xlsx"] }]
        });
        if (files.Count != 1) return;

        var (col, cell, ok) = await ShowImportDialogAsync();
        if (!ok) return;

        _project.ImportText(files[0].Path.LocalPath, col, cell);
        ReloadCurrentStrings();
    }

    // ── Context menu: file list ───────────────────────────────────────────────

    private async void MenuExportAll_Click(object? sender, RoutedEventArgs e)
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select export folder"
        });
        if (folder.Count != 1) return;

        FlushEditsAndSave();
        _project.ExportAll(folder[0].Path.LocalPath);
        await ShowInfoAsync("Done!");
    }

    private async void MenuImportAll_Click(object? sender, RoutedEventArgs e)
    {
        var (col, cell, ok) = await ShowImportDialogAsync();
        if (!ok) return;

        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select folder with .xlsx files"
        });
        if (folder.Count != 1) return;

        FlushEditsAndSave();
        _project.ImportAll(folder[0].Path.LocalPath, col, cell);
        LoadFileList();
        await ShowInfoAsync("Done!");
    }

    private async void MenuTranslateNames_Click(object? sender, RoutedEventArgs e)
    {
        var names = _project.GetAllNames();
        var win = new NamesWindow(names);
        await win.ShowDialog(this);
    }

    private async void MenuLineBreaksAll_Click(object? sender, RoutedEventArgs e)
    {
        string? fontFile = await OpenFontFileAsync();
        if (fontFile == null) return;
        FlushEditsAndSave();
        _project.InsertLineBreaksAll(fontFile, 455);
        ReloadCurrentStrings();
    }

    private void MenuRemoveLineBreaksAll_Click(object? sender, RoutedEventArgs e)
    {
        FlushEditsAndSave();
        _project.RemoveLineBreaksAll();
        ReloadCurrentStrings();
    }

    // ── Help buttons ─────────────────────────────────────────────────────────

    private async void ButtonFilesHelp_Click(object? sender, RoutedEventArgs e) =>
        await ShowInfoAsync(
            "This table contains 363 files with 26508 lines to be translated.\n" +
            "Double-click a file to load it.\n" +
            "You can export and import text from all files from the context menu.\n" +
            "To import the translated text, you need to rename your .xlsx files " +
            "according to the name of the .obj file, for example, \"_0000ESS1.xlsx\".");

    private async void ButtonStringsHelp_Click(object? sender, RoutedEventArgs e) =>
        await ShowInfoAsync(
            "This table contains all the sentences stored in the selected file.\n" +
            "All entered data will be automatically saved for later use.\n" +
            "The brackets \"（\" and \"）\" will be added to the translated text automatically.\n" +
            "You can export all rows to a .xlsx file from the context menu.\n" +
            "You can also import the finished translation from .xlsx tables.");

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ReloadCurrentStrings()
    {
        if (_selectedFileRow == null) return;
        _stringRows.Clear();
        foreach (var r in _project.Strings)
        {
            _stringRows.Add(new StringRow
            {
                Name       = r.Name,
                Sentence   = r.Sentence,
                Translated = r.Translated ?? ""
            });
        }
    }

    private async Task<string?> OpenFontFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select dumped font file",
            FileTypeFilter = [new FilePickerFileType("Text files") { Patterns = ["*.txt"] }]
        });
        return files.Count == 1 ? files[0].Path.LocalPath : null;
    }

    private async Task ShowInfoAsync(string message)
    {
        var dlg = new Window
        {
            Title = "ToradoraTranslateTool",
            Width = 460, Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 420
        });
        var btn = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, MinWidth = 80 };
        var tcs = new TaskCompletionSource();
        btn.Click += (_, _) => { tcs.TrySetResult(); dlg.Close(); };
        panel.Children.Add(btn);
        dlg.Content = panel;
        await dlg.ShowDialog(this);
        await tcs.Task;
    }

    /// <returns>(column, cell, confirmed)</returns>
    private async Task<(int col, int cell, bool ok)> ShowImportDialogAsync()
    {
        var dlg = new ImportDialog();
        bool confirmed = await dlg.ShowDialog<bool>(this);
        return confirmed ? (dlg.Column, dlg.Cell, true) : (0, 0, false);
    }

    private void TranslationWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_selectedFileRow != null)
            FlushEditsAndSave();
    }
}
