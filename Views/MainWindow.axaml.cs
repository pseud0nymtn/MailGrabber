using Avalonia.Controls;
using Avalonia.Interactivity;
using MailGrabber.ViewModels;

namespace MailGrabber.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (this.FindControl<Button>("SettingsButton") is Button settingsButton)
        {
            settingsButton.Click += OnSettingsButtonClick;
        }
    }

    private async void OnSettingsButtonClick(object? sender, RoutedEventArgs e)
    {
        await OpenSettingsDialog();
    }

    private async Task OpenSettingsDialog()
    {
        var settingsViewModel = new SettingsDialogViewModel();

        if (DataContext is MainWindowViewModel mainVmCurrent)
        {
            var current = mainVmCurrent.GetCurrentSettings();
            settingsViewModel.EnableOutlook = current.EnableOutlook;
            settingsViewModel.EnableGmail = current.EnableGmail;
            settingsViewModel.MaxMessages = current.MaxMessages;
            settingsViewModel.OutputPath = current.OutputPath;
            settingsViewModel.JsonOutputPath = current.JsonOutputPath;
            settingsViewModel.HtmlOutputPath = current.HtmlOutputPath;
        }

        var settingsWindow = new SettingsWindow { DataContext = settingsViewModel };

        var apply = false;

        settingsViewModel.OnOk = () =>
        {
            apply = true;
            settingsWindow.Close();
        };

        settingsViewModel.OnCancel = () =>
        {
            settingsWindow.Close();
        };

        await settingsWindow.ShowDialog(this);

        if (apply && DataContext is MainWindowViewModel mainVm)
        {
            mainVm.ApplySettings(
                settingsViewModel.EnableOutlook,
                settingsViewModel.EnableGmail,
                settingsViewModel.MaxMessages,
                settingsViewModel.OutputPath,
                settingsViewModel.JsonOutputPath,
                settingsViewModel.HtmlOutputPath
            );
        }
    }
}

