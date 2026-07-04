using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using Avalonia.Platform.Storage;

namespace OreimoTranslateToolAvalonia;

public partial class MainWindow : Window {
    public static string StartupPath { get; private set; } = AppContext.BaseDirectory;
    public static string DataDir = Path.Combine(StartupPath, "Data");

    private readonly RyuujiApi.RyuujiApi api;
    private readonly DispatcherTimer _timerWork;
    private bool _isWorking;

    public MainWindow() {
        Console.WriteLine($"StartupPath is: {StartupPath}");
        Console.WriteLine();

        InitializeComponent();
        DataDir = Path.Combine(StartupPath, "Data");
        api = new RyuujiApi.RyuujiApi(StartupPath);
        
        string gameName = api.DetectGameFromIso(Path.Combine(DataDir, "Iso"));
        string randomNumber = RandomNumberGenerator.GetInt32(1,3).ToString();
        catImage.Source = gameName switch {
            "Oreimo" => new Avalonia.Media.Imaging.Bitmap(Path.Combine(StartupPath, "Assets", "kuroneko" + randomNumber + ".jpg")),
            "Toradora" => new Avalonia.Media.Imaging.Bitmap(Path.Combine(StartupPath, "Assets", "Taiga.png")),
        };
        
        // Version label
        labelVersion.Text = System.Reflection.Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version
            .ToString(3);

        // Animated "Working..." timer
        _timerWork = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timerWork.Tick += TimerWork_Tick;

        EnableButtons();
        Closing += MainWindow_Closing;
    }

    // ── Button state management ──────────────────────────────────────────────

    private void EnableButtons() {
        buttonExtractIso.IsEnabled = true;
        bool isoExtracted = File.Exists(Path.Combine(DataDir, "Iso", "PSP_GAME", "USRDIR", "resource.dat")) ||
                            File.Exists(Path.Combine(DataDir, "Iso", "PSP_GAME", "INSDIR", "RES.DAT"));

        if (isoExtracted) {
            buttonExtractGame.IsEnabled = true;
            buttonExtractGame.IsVisible = true;
            
            buttonDeleteGenRes.IsEnabled = false;
            buttonDeleteGenRes.IsVisible = false;
            
            buttonStartGame.IsEnabled = true;
        }

        bool gameExtracted = File.Exists(Path.Combine(DataDir, "Txt", "utf16.txt", "utf16.txt")) ||
                             File.Exists(Path.Combine(DataDir, "Extracted", "first", "seekmap", "res.map.gz"));

        if (gameExtracted) {
            buttonExtractGame.IsEnabled = false;
            buttonExtractGame.IsVisible = false;
            
            buttonDeleteGenRes.IsEnabled = true;
            buttonDeleteGenRes.IsVisible = true;

            buttonTranslate.IsEnabled = true;
            buttonRepackGame.IsEnabled = true;
            buttonExportGame.IsEnabled = true;
        }
    }

    private void DisableButtons() {
        buttonExtractIso.IsEnabled = false;
        buttonExtractGame.IsEnabled = false;
        buttonDeleteGenRes.IsEnabled = false;
        buttonTranslate.IsEnabled = false;
        buttonRepackGame.IsEnabled = false;
        buttonStartGame.IsEnabled = false;
        buttonExportGame.IsEnabled = false;
        
        buttonExtractGame.IsVisible = true;
        buttonDeleteGenRes.IsVisible = false;
    }

    private void SetWorking(bool isWorking) {
        _isWorking = isWorking;
        if (isWorking) {
            labelWork.Text = "Working";
            _timerWork.Start();
        } else {
            _timerWork.Stop();
            labelWork.Text = "Ready";
        }
    }

    private void TimerWork_Tick(object? sender, EventArgs e) {
        labelWork.Text = labelWork.Text == "Working..." ? "Working" : labelWork.Text + ".";
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string?> OpenIsoFileAsync() {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Select ISO file",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("ISO files") { Patterns = ["*.iso"] }]
        });
        return files.Count == 1 ? files[0].Path.LocalPath : null;
    }

    private async Task<string?> SaveIsoFileAsync() {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title = "Save ISO file",
            DefaultExtension = "iso",
            FileTypeChoices = [new FilePickerFileType("ISO files") { Patterns = ["*.iso"] }]
        });
        return file?.Path.LocalPath;
    }

    private async Task ShowErrorAsync(string message, long ms = 0) =>
        await ShowDialogAsync("Error" + (ms > 0 ? $" in {ms} ms!" : ""), message, 400);

    private async Task ShowInfoAsync(string message, int height = 100) =>
        await ShowDialogAsync("Toradora & Oreimo Translate Tool", message, height);

    private async Task ShowDialogAsync(string title, string message, int height) {
        var dialog = new Window {
            Title = title,
            Width = 500,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true
        };
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 380
        });
        var okBtn = new Button {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            MinWidth = 80
        };
        var tcs = new TaskCompletionSource();
        okBtn.Click += (_, _) => {
            tcs.TrySetResult();
            dialog.Close();
        };
        panel.Children.Add(okBtn);
        dialog.Content = panel;
        await dialog.ShowDialog(this);
        await tcs.Task;
    }

    private static void LogError(Exception e) => Console.Error.WriteLine(e);

    // ── Button handlers ──────────────────────────────────────────────────────

    private async void ButtonExtractIso_Click(object? sender, RoutedEventArgs e) {
        string? path = await OpenIsoFileAsync();
        if (path == null) return;

        var sw = Stopwatch.StartNew();
        try {
            SetWorking(true);
            DisableButtons();

            IsoProgress.Value = 0;
            IsoProgress.IsVisible = true;
            ExtractIsoHelpButton.IsVisible = false;
            buttonExtractIso.IsVisible = false;

            // Run on a background thread to avoid deadlocking the UI thread,
            // since IsoTools.ExtractIso uses GetAwaiter().GetResult() internally.
            await Task.Run(() => api.ExtractIso(
                path,
                file => Console.WriteLine($"\t{file}"),
                progress => Dispatcher.UIThread.Post(() => IsoProgress.Value = progress)
            ).GetAwaiter().GetResult());

            await ShowInfoAsync($"ISO extraction completed in {sw.ElapsedMilliseconds} ms.");
        } catch (Exception ex) {
            LogError(ex);
            await ShowErrorAsync(ex.ToString(), sw.ElapsedMilliseconds);
        } finally {
            sw.Stop();
            SetWorking(false);
            EnableButtons();
            IsoProgress.IsVisible = false;
            buttonExtractIso.IsVisible = true;
            ExtractIsoHelpButton.IsVisible = true;
            IsoProgress.Value = 0;
        }
    }

    private async void ButtonExtractGame_Click(object? sender, RoutedEventArgs e) {
        var sw = Stopwatch.StartNew();
        try {
            SetWorking(true);
            DisableButtons();

            await api.ExtractGame(DataDir);

            await ShowInfoAsync($"Game files extraction completed in {sw.ElapsedMilliseconds} ms.");
        } catch (Exception ex) {
            LogError(ex);
            await ShowErrorAsync(ex.ToString(), sw.ElapsedMilliseconds);
        } finally {
            sw.Stop();
            SetWorking(false);
            EnableButtons();
        }
    }

    private async void ButtonDeleteGenRes_Click(object? sender, RoutedEventArgs e) {
        // Confirm dialog
        var confirmed = await ShowConfirmAsync(
            "This is an effective factory reset.\nAre you sure you want to continue?");
        if (!confirmed) return;

        var sw = Stopwatch.StartNew();
        try {
            SetWorking(true);
            DisableButtons();

            bool repacked = File.Exists(Path.Combine(DataDir, "Extracted", "-"));
            var tasks = new Task[] {
                Task.Run(() => Directory.Delete(Path.Combine(DataDir, "Extracted"), true)),
                Task.Run(() => Directory.Delete(Path.Combine(DataDir, "Obj"), true)),
                Task.Run(() => {
                    if (Directory.Exists(Path.Combine(DataDir, "Txt"))) {
                        Directory.Delete(Path.Combine(DataDir, "Txt"), true);
                    }
                }),
                Task.CompletedTask
            };
            if (repacked)
                tasks[3] = Task.Run(() => Directory.Delete(Path.Combine(DataDir, "Iso"), true));

            await Task.WhenAll(tasks);
            await ShowInfoAsync($"Resource deletion completed in {sw.ElapsedMilliseconds} ms.");
        } catch (Exception ex) {
            LogError(ex);
            await ShowErrorAsync(ex.ToString(), sw.ElapsedMilliseconds);
        } finally {
            sw.Stop();
            SetWorking(false);
            EnableButtons();
        }
    }

    private void ButtonTranslate_Click(object? sender, RoutedEventArgs e) {
        var win = new TranslationWindow(StartupPath);
        win.Show();
    }

    private async void ButtonRepackGame_Click(object? sender, RoutedEventArgs e) {
        bool debugMode = itemDebugMode.IsChecked == true;

        var sw = Stopwatch.StartNew();
        try {
            SetWorking(true);
            DisableButtons();

            await api.RepackGame(DataDir, debugMode);

            await ShowInfoAsync($"Game files repacking completed in {sw.ElapsedMilliseconds} ms.");
        } catch (Exception ex) {
            LogError(ex);
            await ShowErrorAsync(ex.ToString(), sw.ElapsedMilliseconds);
        } finally {
            sw.Stop();
            SetWorking(false);
            EnableButtons();
        }
    }

    private async void ButtonStartGame_Click(object? sender, RoutedEventArgs e) {
        var sw = Stopwatch.StartNew();
        try {
            SetWorking(true);
            DisableButtons();
            await api.StartGame();
        } catch (Exception ex) {
            LogError(ex);
            await ShowErrorAsync(ex.ToString(), sw.ElapsedMilliseconds);
        } finally {
            sw.Stop();
            SetWorking(false);
            EnableButtons();
        }
    }

    private async void ButtonExportGame_Click(object? sender, RoutedEventArgs e) {
        string? selectedPath = await SaveIsoFileAsync();
        if (selectedPath == null) return;

        var sw = Stopwatch.StartNew();
        try {
            SetWorking(true);
            DisableButtons();
            ExtractProgress.IsVisible = true;
            buttonExportGame.IsVisible = false;
            ExportGameHelpButton.IsVisible = false;
            ExtractProgress.Value = 0;

            string isoPath = Path.Combine(DataDir, "Iso");
            string mkisofs = "mkisofs";
            string mkisofsConf = Path.Combine(AppContext.BaseDirectory, "mkisofs.conf");
            if (File.Exists(mkisofsConf))
                mkisofs = await File.ReadAllTextAsync(mkisofsConf);

            await Task.Run(() => api.RepackIso(mkisofs, isoPath, selectedPath,
                progress => Dispatcher.UIThread.Post(() => ExtractProgress.Value = (int)progress)));

            await ShowInfoAsync($"Game export completed in {sw.ElapsedMilliseconds} ms.");
        } catch (Exception ex) {
            LogError(ex);
            await ShowErrorAsync(ex.ToString(), sw.ElapsedMilliseconds);
        } finally {
            sw.Stop();
            SetWorking(false);
            EnableButtons();
            ExtractProgress.IsVisible = false;
            buttonExportGame.IsVisible = true;
            ExportGameHelpButton.IsVisible = true;
            ExtractProgress.Value = 0;
        }
    }

    // ── Help buttons ─────────────────────────────────────────────────────────

    private async void ButtonExtractIsoHelp_Click(object? sender, RoutedEventArgs e) =>
        await ShowInfoAsync("This stage will extract selected ISO file to the \\Data\\Iso\\ folder");

    private async void ButtonExtractGameHelp_Click(object? sender, RoutedEventArgs e) =>
        await ShowInfoAsync(
            "This stage will extract and process .dat files from ISO.\nIt'll take ~40 seconds depending on the CPU");

    private async void ButtonTranslateHelp_Click(object? sender, RoutedEventArgs e) =>
        await ShowInfoAsync("At this stage you will be able to translate the game text, including menus and settings");

    private async void ButtonRepackGameHelp_Click(object? sender, RoutedEventArgs e) =>
        await ShowInfoAsync(
            "This stage will inject translation and repack all game files.\n" +
            "It'll take ~5-10 seconds depending on the SSD.\n" +
            "You can enable debug mode by right-clicking on the repack button.\n" +
            "In this mode you will be able to teleport to any level, and much more", 160);

    private async void ButtonStartGameHelp_Click(object? sender, RoutedEventArgs e) =>
        await ShowInfoAsync(RyuujiApi.RyuujiApi.StartGameHelpText, 250);

    private async void ButtonRepackIsoHelp_Click(object? sender, RoutedEventArgs e) =>
        await ShowInfoAsync("This stage will repack ISO and save it in the program folder");

    // ── Confirm dialog helper ─────────────────────────────────────────────────

    private async Task<bool> ShowConfirmAsync(string message) {
        bool result = false;
        var dialog = new Window {
            Title = "Toradora & Oreimo Translate Tool",
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 380
        });
        var btnPanel = new StackPanel {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8
        };
        var yesBtn = new Button { Content = "Yes", MinWidth = 70 };
        var noBtn = new Button { Content = "No", MinWidth = 70 };
        yesBtn.Click += (_, _) => {
            result = true;
            dialog.Close();
        };
        noBtn.Click += (_, _) => {
            result = false;
            dialog.Close();
        };
        btnPanel.Children.Add(yesBtn);
        btnPanel.Children.Add(noBtn);
        panel.Children.Add(btnPanel);
        dialog.Content = panel;
        await dialog.ShowDialog(this);
        return result;
    }

    // ── Window closing ────────────────────────────────────────────────────────

    private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e) {
        if (!_isWorking) return;
        e.Cancel = true;
        bool close = await ShowConfirmAsync(
            "There's an ongoing task.\nAre you sure you want to close the app?");
        if (close) {
            _isWorking = false;
            Close();
        }
    }
}