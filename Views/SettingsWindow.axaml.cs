using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MailGrabber.ViewModels;

namespace MailGrabber.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (this.FindControl<Button>("SaveAsButton") is Button saveAsButton)
        {
            saveAsButton.Click += OnSaveAsButtonClick;
        }
    }

    private async void OnSaveAsButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsDialogViewModel vm)
        {
            return;
        }

        var topLevel = GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } provider)
        {
            return;
        }

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Settings As",
            DefaultExtension = "json",
            SuggestedFileName = "appsettings.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON File") { Patterns = new[] { "*.json" } }
            }
        });

        if (file != null)
        {
            vm.SaveToPath(file.Path.LocalPath);
        }
    }
}
