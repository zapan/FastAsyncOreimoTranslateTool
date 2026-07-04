using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TranslateCLI;

namespace OreimoTranslateToolAvalonia;

public partial class TranslationWindow : Window {
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
        public string Name { get; set; } = "";
        public string Sentence { get; set; } = "";
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

    private readonly string _basePath;
    private readonly TranslationProjectCli _project;
    private readonly ObservableCollection<FileRow> _fileRows = [];
    private readonly ObservableCollection<StringRow> _stringRows = [];
    private FileRow? _selectedFileRow;
    private bool _isBusy;

    // ── Constructor ───────────────────────────────────────────────────────────

    public TranslationWindow(string basePath)
    {
        InitializeComponent();

        _basePath = basePath;
        _project = new TranslationProjectCli(basePath);

        dataGridFiles.ItemsSource = _fileRows;
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
                FileName = fp.FileName,
                ProgressText = fp.TranslationPercent + "%",
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
        _fileRows[0].ProgressText = pct + "%";
        _fileRows[0].ProgressValue = pct;
    }

    private void UpdateRowPercent(string fileName, double percent)
    {
        var row = _fileRows.FirstOrDefault(r =>
            r.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (row == null) return;
        row.ProgressText = percent + "%";
        row.ProgressValue = percent;
        UpdateTotalPercent();
        // Refresh the DataGrid cell
        int idx = _fileRows.IndexOf(row);
        if (idx >= 0) { _fileRows.RemoveAt(idx); _fileRows.Insert(idx, row); }
    }

    // ── Load a file into the strings grid ────────────────────────────────────

    /// <summary>Load a file on a background thread, then refresh the UI.</summary>
    private async Task LoadFileAsync(FileRow fileRow)
    {
        // Flush previous edits first (also sync, but fast for one file)
        if (_selectedFileRow != null)
            await FlushEditsAndSaveAsync();

        _selectedFileRow = fileRow;

        await Task.Run(() => _project.LoadFile(fileRow.FileName));

        // Update UI on UI thread
        _stringRows.Clear();
        foreach (var r in _project.Strings)
        {
            _stringRows.Add(new StringRow
            {
                Name = r.Name,
                Sentence = r.Sentence,
                Translated = r.Translated ?? ""
            });
        }
    }

    /// <summary>Push StringRow edits back into _project.Strings, then save on background thread.</summary>
    private async Task FlushEditsAndSaveAsync()
    {
        // Snapshot the translated values on the UI thread before going async
        var translated = _stringRows.Select(r => r.Translated).ToArray();
        var fileName = _selectedFileRow?.FileName;

        await Task.Run(() =>
        {
            for (int i = 0; i < translated.Length && i < _project.Strings.Count; i++)
                _project.Strings[i].Translated = translated[i];
            _project.SaveProgress();
        });

        if (fileName != null)
        {
            var fp = _project.Files.FirstOrDefault(f =>
                f.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            if (fp != null)
                UpdateRowPercent(fileName, fp.TranslationPercent);
        }
    }

    /// <summary>Sync version used only from the Closing event (can't await there).</summary>
    private void FlushEditsAndSaveSync()
    {
        for (int i = 0; i < _stringRows.Count && i < _project.Strings.Count; i++)
            _project.Strings[i].Translated = _stringRows[i].Translated;
        _project.SaveProgress();
    }

    private void ReloadCurrentStrings()
    {
        _stringRows.Clear();
        foreach (var r in _project.Strings)
        {
            _stringRows.Add(new StringRow
            {
                Name = r.Name,
                Sentence = r.Sentence,
                Translated = r.Translated ?? ""
            });
        }
    }

    // ── DataGrid events ───────────────────────────────────────────────────────

    private async void DataGridFiles_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (_isBusy) return;
        if (dataGridFiles.SelectedItem is FileRow row && row != _fileRows[0])
            await RunBusyAsync(() => LoadFileAsync(row));
    }

    private void DataGridStrings_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        // Two-way binding handles updates; nothing extra needed here
    }

    // ── Busy helper (disables interactions while working) ─────────────────────

    private async Task RunBusyAsync(Func<Task> work)
    {
        _isBusy = true;
        dataGridFiles.IsEnabled = false;
        dataGridStrings.IsEnabled = false;
        try { await work(); }
        finally
        {
            _isBusy = false;
            dataGridFiles.IsEnabled = true;
            dataGridStrings.IsEnabled = true;
        }
    }

    // ── Context menu: strings grid ────────────────────────────────────────────

    private async void MenuLineBreaks_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedFileRow == null) { await ShowInfoAsync("First select the file!"); return; }
        string? fontFile = await OpenFontFileAsync();
        if (fontFile == null) return;

        await RunBusyAsync(async () =>
        {
            await FlushEditsAndSaveAsync();
            // InsertLineBreaksAll iterates all files — run on background thread
            await Task.Run(() => _project.InsertLineBreaksAll(fontFile, 455));
            ReloadCurrentStrings();
        });
    }

    private async void MenuRemoveLineBreaks_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedFileRow == null) return;
        await RunBusyAsync(async () =>
        {
            await FlushEditsAndSaveAsync();
            await Task.Run(() => _project.RemoveLineBreaks());
            ReloadCurrentStrings();
        });
    }

    private async void MenuExportStrings_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedFileRow == null) { await ShowInfoAsync("First select the file!"); return; }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export strings",
            DefaultExtension = "xlsx",
            FileTypeChoices = [new FilePickerFileType("Excel sheet") { Patterns = ["*.xlsx"] }],
            SuggestedFileName = Path.GetFileNameWithoutExtension(_selectedFileRow.FileName)
        });
        if (file == null) return;

        await RunBusyAsync(async () =>
        {
            await FlushEditsAndSaveAsync();
            await Task.Run(() => _project.ExportText(file.Path.LocalPath));
        });
    }

    private async void MenuImportStrings_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedFileRow == null) { await ShowInfoAsync("First select the file!"); return; }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import strings",
            FileTypeFilter = [new FilePickerFileType("Excel sheet") { Patterns = ["*.xlsx"] }]
        });
        if (files.Count != 1) return;

        var (col, cell, ok) = await ShowImportDialogAsync();
        if (!ok) return;

        string path = files[0].Path.LocalPath;
        await RunBusyAsync(async () =>
        {
            await Task.Run(() => _project.ImportText(path, col, cell));
            ReloadCurrentStrings();
        });
    }

    // ── Context menu: file list ───────────────────────────────────────────────

    private async void MenuExportAll_Click(object? sender, RoutedEventArgs e)
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select export folder"
        });
        if (folder.Count != 1) return;

        string folderPath = folder[0].Path.LocalPath;
        await RunBusyAsync(async () =>
        {
            await FlushEditsAndSaveAsync();
            await Task.Run(() => _project.ExportAll(folderPath));
            await ShowInfoAsync("Done!");
        });
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

        string folderPath = folder[0].Path.LocalPath;
        await RunBusyAsync(async () =>
        {
            await FlushEditsAndSaveAsync();
            await Task.Run(() => _project.ImportAll(folderPath, col, cell));
            // Reload the file list so percentages update
            Dispatcher.UIThread.Post(LoadFileList);
            await ShowInfoAsync("Done!");
        });
    }

    private async void MenuTranslateNames_Click(object? sender, RoutedEventArgs e)
    {
        // GetAllNames reads many .obj files — run on background thread
        var names = await Task.Run(() => _project.GetAllNames());
        var win = new NamesWindow(_basePath, names);
        await win.ShowDialog(this);
    }

    private async void MenuLineBreaksAll_Click(object? sender, RoutedEventArgs e)
    {
        string? fontFile = await OpenFontFileAsync();
        if (fontFile == null) return;

        await RunBusyAsync(async () =>
        {
            await FlushEditsAndSaveAsync();
            await Task.Run(() => _project.InsertLineBreaksAll(fontFile, 455));
            ReloadCurrentStrings();
        });
    }

    private async void MenuRemoveLineBreaksAll_Click(object? sender, RoutedEventArgs e)
    {
        await RunBusyAsync(async () =>
        {
            await FlushEditsAndSaveAsync();
            await Task.Run(() => _project.RemoveLineBreaksAll());
            ReloadCurrentStrings();
        });
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
            Title = "Toradora & Oreimo Translate Tool",
            Width = 460,
            Height = 160,
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
        var btn = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            MinWidth = 80
        };
        var tcs = new TaskCompletionSource();
        btn.Click += (_, _) => { tcs.TrySetResult(); dlg.Close(); };
        panel.Children.Add(btn);
        dlg.Content = panel;
        await dlg.ShowDialog(this);
        await tcs.Task;
    }

    private async Task<(int col, int cell, bool ok)> ShowImportDialogAsync()
    {
        var dlg = new ImportDialog();
        bool confirmed = await dlg.ShowDialog<bool>(this);
        return confirmed ? (dlg.Column, dlg.Cell, true) : (0, 0, false);
    }

    private void TranslationWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_selectedFileRow != null)
            FlushEditsAndSaveSync();
    }
}
