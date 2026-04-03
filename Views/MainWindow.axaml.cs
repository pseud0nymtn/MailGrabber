using Avalonia.Controls;
using Avalonia.Input;
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

        if (this.FindControl<ListBox>("ClusterFilterList") is ListBox clusterFilterList)
        {
            clusterFilterList.KeyDown += OnClusterFilterListKeyDown;
        }
    }

    private void OnClusterFilterListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.SelectedFilterCluster is null)
        {
            return;
        }

        vm.SelectedFilterCluster.IsSelected = !vm.SelectedFilterCluster.IsSelected;
        e.Handled = true;
    }

    private void OnRunLogTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        textBox.CaretIndex = textBox.Text?.Length ?? 0;
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
            settingsViewModel.EnableNewsletterClustering = current.EnableNewsletterClustering;
            settingsViewModel.MaxMessages = current.MaxMessages;
            settingsViewModel.ConfigPath = current.ConfigPath;
            settingsViewModel.OutputPath = current.OutputPath;
            settingsViewModel.JsonOutputPath = current.JsonOutputPath;
            settingsViewModel.HtmlOutputPath = current.HtmlOutputPath;
            settingsViewModel.BaseSettings = mainVmCurrent.LastLoadedSettings;
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
                settingsViewModel.EnableNewsletterClustering,
                settingsViewModel.ConfigPath,
                settingsViewModel.OutputPath,
                settingsViewModel.JsonOutputPath,
                settingsViewModel.HtmlOutputPath
            );

            // Persist the changes to disk so they survive an app restart.
            settingsViewModel.SaveToPath(settingsViewModel.ConfigPath);
        }
    }
}

