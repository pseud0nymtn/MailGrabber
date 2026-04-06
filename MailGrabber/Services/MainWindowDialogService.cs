using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MailGrabber.Models;
using MailGrabber.ViewModels;
using MailGrabber.Views;

namespace MailGrabber.Services;

public interface IMainWindowDialogService
{
    Task<ClusterReport?> OpenReportJsonAsync();

    Task<bool> ExportDomainsAsync(IReadOnlyList<string> domains);

    Task<MainWindowSettingsState?> OpenSettingsDialogAsync(MainWindowSettingsState current, AppSettings? baseSettings);
}

public sealed class MainWindowSettingsState
{
    public bool EnableOutlook { get; init; }

    public bool EnableGmail { get; init; }

    public int MaxMessages { get; init; }

    public int OldestMessageAgeDays { get; init; }

    public bool EnableNewsletterClustering { get; init; }

    public string ConfigPath { get; init; } = "appsettings.json";

    public string ClientId { get; init; } = "YOUR-CLIENT-ID-HERE";

    public string GmailClientSecretsPath { get; init; } = "google-client-secret.json";

    public string OutputPath { get; init; } = "output/sender-clusters.csv";

    public string JsonOutputPath { get; init; } = "output/sender-clusters.json";

    public string HtmlOutputPath { get; init; } = "output/cluster-viewer.html";
}

public sealed class MainWindowDialogService(Window owner) : IMainWindowDialogService
{
    public async Task<ClusterReport?> OpenReportJsonAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "sender-clusters.json öffnen",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("JSON-Dateien") { Patterns = ["*.json"] },
                new FilePickerFileType("Alle Dateien") { Patterns = ["*"] }
            ]
        });

        if (files.Count == 0)
        {
            return null;
        }

        await using var stream = await files[0].OpenReadAsync();
        return await JsonSerializer.DeserializeAsync<ClusterReport>(stream);
    }

    public async Task<bool> ExportDomainsAsync(IReadOnlyList<string> domains)
    {
        if (domains.Count == 0)
        {
            return false;
        }

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export marked domains",
            SuggestedFileName = "marked-domains.txt",
            FileTypeChoices =
            [
                new FilePickerFileType("Text file") { Patterns = ["*.txt"] },
                new FilePickerFileType("All files") { Patterns = ["*"] }
            ]
        });

        if (file is null)
        {
            return false;
        }

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        foreach (var domain in domains)
        {
            await writer.WriteLineAsync(domain);
        }

        return true;
    }

    public async Task<MainWindowSettingsState?> OpenSettingsDialogAsync(MainWindowSettingsState current, AppSettings? baseSettings)
    {
        var settingsViewModel = new SettingsDialogViewModel
        {
            EnableOutlook = current.EnableOutlook,
            EnableGmail = current.EnableGmail,
            EnableNewsletterClustering = current.EnableNewsletterClustering,
            MaxMessages = current.MaxMessages,
            OldestMessageAgeDays = current.OldestMessageAgeDays,
            ConfigPath = current.ConfigPath,
            ClientId = current.ClientId,
            GmailClientSecretsPath = current.GmailClientSecretsPath,
            OutputPath = current.OutputPath,
            JsonOutputPath = current.JsonOutputPath,
            HtmlOutputPath = current.HtmlOutputPath,
            BaseSettings = baseSettings
        };

        var settingsWindow = new SettingsWindow { DataContext = settingsViewModel };
        var apply = false;

        settingsViewModel.OnOk = () =>
        {
            apply = true;
            settingsWindow.Close();
        };

        settingsViewModel.OnCancel = () => settingsWindow.Close();

        await settingsWindow.ShowDialog(owner);

        if (!apply)
        {
            return null;
        }

        settingsViewModel.SaveToPath(settingsViewModel.ConfigPath);

        return new MainWindowSettingsState
        {
            EnableOutlook = settingsViewModel.EnableOutlook,
            EnableGmail = settingsViewModel.EnableGmail,
            MaxMessages = settingsViewModel.MaxMessages,
            EnableNewsletterClustering = settingsViewModel.EnableNewsletterClustering,
            ConfigPath = settingsViewModel.ConfigPath,
            OldestMessageAgeDays = settingsViewModel.OldestMessageAgeDays,
            ClientId = settingsViewModel.ClientId,
            GmailClientSecretsPath = settingsViewModel.GmailClientSecretsPath,
            OutputPath = settingsViewModel.OutputPath,
            JsonOutputPath = settingsViewModel.JsonOutputPath,
            HtmlOutputPath = settingsViewModel.HtmlOutputPath
        };
    }
}
