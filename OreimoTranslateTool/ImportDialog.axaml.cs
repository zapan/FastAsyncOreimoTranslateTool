using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OreimoTranslateTool;

public partial class ImportDialog : Window
{
    public int Column { get; private set; } = 3;
    public int Cell   { get; private set; } = 1;

    public ImportDialog()
    {
        InitializeComponent();
    }

    private void ButtonOk_Click(object? sender, RoutedEventArgs e)
    {
        Column = (int)(inputColumn.Value ?? 3);
        Cell   = (int)(inputCell.Value   ?? 1);
        Close(true);
    }

    private void ButtonCancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
