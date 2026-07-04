using Avalonia.Controls;
using Avalonia.Interactivity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace OreimoTranslateToolAvalonia;

public partial class NamesWindow : Window
{
    public class NameRow
    {
        public string Original   { get; set; } = "";
        public string Translated { get; set; } = "";
    }

    private readonly string _mainFilePath;
    private readonly ObservableCollection<NameRow> _rows = [];

    public NamesWindow(List<string> originalNames)
    {
        InitializeComponent();

        _mainFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "Translation.json");

        JObject mainFile = JObject.Parse(File.ReadAllText(_mainFilePath));

        foreach (string name in originalNames)
        {
            string translated = "";
            if (mainFile["names"]?[name] != null)
                translated = mainFile["names"]![name]!.ToString();

            _rows.Add(new NameRow { Original = name, Translated = translated });
        }

        dataGridNames.ItemsSource = _rows;
        Closing += NamesWindow_Closing;

        // Warn the user after the window loads
        Opened += async (_, _) =>
        {
            await ShowInfoAsync(
                "Do not translate the names unless you have translated the charaname.txt file, " +
                "otherwise they will not appear in dialogs!");
        };
    }

    private void NamesWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        SaveProgress();
    }

    private void SaveProgress()
    {
        JObject mainFile = JObject.Parse(File.ReadAllText(_mainFilePath));

        if (mainFile["names"] != null)
        {
            foreach (var row in _rows)
                mainFile["names"]![row.Original] = row.Translated;
        }
        else
        {
            JObject translatedNames = new();
            foreach (var row in _rows)
                translatedNames.Add(row.Original, row.Translated);
            mainFile.Add(new JProperty("names", translatedNames));
        }

        File.WriteAllText(_mainFilePath, mainFile.ToString());
    }

    private async void ButtonHelp_Click(object? sender, RoutedEventArgs e) =>
        await ShowInfoAsync(
            "Once you translate the names here, they will be automatically replaced " +
            "in each translated .obj file at the repacking stage.\n" +
            "Be sure to translate the names in the charaname.txt file.");

    private async System.Threading.Tasks.Task ShowInfoAsync(string message)
    {
        var dlg = new Window
        {
            Title = "ToradoraTranslateTool",
            Width = 460, Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };
        var panel = new Avalonia.Controls.StackPanel
            { Margin = new Avalonia.Thickness(16), Spacing = 12 };
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
        var tcs = new System.Threading.Tasks.TaskCompletionSource();
        btn.Click += (_, _) => { tcs.TrySetResult(); dlg.Close(); };
        panel.Children.Add(btn);
        dlg.Content = panel;
        await dlg.ShowDialog(this);
        await tcs.Task;
    }
}
