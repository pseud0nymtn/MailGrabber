using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MailGrabber.ViewModels;
using System.Diagnostics.CodeAnalysis;

namespace MailGrabber.Views;

[ExcludeFromCodeCoverage]
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        ApplyMaxHeightFromCurrentScreen();

        if (this.FindControl<Button>("SaveAsButton") is Button saveAsButton)
        {
            saveAsButton.Click += OnSaveAsButtonClick;
        }
    }

    private void ApplyMaxHeightFromCurrentScreen()
    {
        var topLevel = GetTopLevel(this);
        if (topLevel?.Screens is null)
        {
            return;
        }

        var screen = topLevel.Screens.ScreenFromVisual(this) ?? topLevel.Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var renderScaling = topLevel.RenderScaling;
        if (renderScaling <= 0)
        {
            renderScaling = 1.0;
        }

        var maxHeightDip = (screen.WorkingArea.Height / renderScaling) * 0.9;
        MaxHeight = maxHeightDip;
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
