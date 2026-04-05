using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MailGrabber.Models;
using MailGrabber.Services;

namespace MailGrabber.ViewModels;

[ExcludeFromCodeCoverage]
public partial class MainWindowViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions UiJsonOptions = new() { WriteIndented = true };
    private static readonly string UiSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MailGrabber",
        "ui-settings.json");

    private string _lastHtmlOutputPath = "output/cluster-viewer.html";
    private ClusterReport? _fullReport;
    private bool _enableOutlookOverride = true;
    private bool _enableGmailOverride;
    private bool _enableNewsletterClusteringOverride = true;
    private int _maxMessagesOverride = 2000;
    private string _clientIdOverride = "YOUR-CLIENT-ID-HERE";
    private string _gmailClientSecretsPathOverride = "google-client-secret.json";
    private string _outputPathOverride = "output/sender-clusters.csv";
    private string _jsonOutputPathOverride = "output/sender-clusters.json";
    private string _htmlOutputPathOverride = "output/cluster-viewer.html";
    private AppSettings? _lastLoadedSettings;
    private readonly IMainWindowDialogService _dialogService;

    [ObservableProperty]
    private string configPath = "appsettings.json";

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string runLog = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ClusterBucketViewModel? selectedCluster;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool showOnlyMarkedClusters;

    [ObservableProperty]
    private SelectableClusterViewModel? selectedFilterCluster;

    [ObservableProperty]
    private int markedClusterCount;

    [ObservableProperty]
    private bool showSendersFromAllMarkedClusters;

    [ObservableProperty]
    private bool sortSendersByDomain;

    [ObservableProperty]
    private bool sortClustersAlphabetically;

    [ObservableProperty]
    private string selectedThemeMode = "System";

    public MainWindowViewModel()
        : this(new NullMainWindowDialogService())
    {
    }

    public MainWindowViewModel(IMainWindowDialogService dialogService)
    {
        _dialogService = dialogService;

        Clusters = new ObservableCollection<ClusterBucketViewModel>();
        AllClusters = new ObservableCollection<SelectableClusterViewModel>();
        FilterClusters = new ObservableCollection<SelectableClusterViewModel>();
        SenderRows = new ObservableCollection<SenderRowViewModel>();

        RunCommand = new AsyncRelayCommand(RunAsync, CanRun);
        OpenJsonCommand = new AsyncRelayCommand(OpenJsonAsync, CanUseDialogCommands);
        ExportDomainsCommand = new AsyncRelayCommand(ExportDomainsAsync, CanExportDomains);
        OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsAsync, CanUseDialogCommands);
        OpenHtmlViewerCommand = new RelayCommand(OpenHtmlViewer, CanOpenHtmlViewer);
        OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder);
        ClearFilterCommand = new RelayCommand(ClearFilter);
        ToggleSelectedFilterClusterCommand = new RelayCommand(ToggleSelectedFilterCluster);

        ThemeModeOptions = ["System", "Hell", "Dunkel"];

        LoadSettingsFromConfig();
        LoadUiSettings();
        ApplyThemeMode(SelectedThemeMode);
    }

    public ObservableCollection<ClusterBucketViewModel> Clusters { get; }

    public ObservableCollection<SelectableClusterViewModel> AllClusters { get; }

    public ObservableCollection<SelectableClusterViewModel> FilterClusters { get; }

    public ObservableCollection<SenderRowViewModel> SenderRows { get; }

    public IAsyncRelayCommand RunCommand { get; }

    public IAsyncRelayCommand OpenJsonCommand { get; }

    public IAsyncRelayCommand ExportDomainsCommand { get; }

    public IAsyncRelayCommand OpenSettingsCommand { get; }

    public IRelayCommand OpenHtmlViewerCommand { get; }

    public IRelayCommand OpenOutputFolderCommand { get; }

    public IRelayCommand ClearFilterCommand { get; }

    public IRelayCommand ToggleSelectedFilterClusterCommand { get; }

    public IReadOnlyList<string> ThemeModeOptions { get; }

    public AppSettings? LastLoadedSettings => _lastLoadedSettings;

    public void LoadReportFromJson(ClusterReport report)
    {
        LoadReport(report);
        StatusMessage = $"Report geladen: {report.Clusters.Count} Cluster, {report.TotalInputMessages} Nachrichten.";
    }

    public IReadOnlyList<string> GetMarkedDomains()
    {
        var markedClusterNames = AllClusters
            .Where(c => c.IsSelected)
            .Select(c => c.Cluster)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _fullReport?.Clusters
            .Where(b => markedClusterNames.Contains(b.Cluster))
            .SelectMany(b => b.SenderAddresses)
            .Select(s => s.Domain)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? [];
    }

    partial void OnIsBusyChanged(bool value)
    {
        RunCommand.NotifyCanExecuteChanged();
        OpenJsonCommand.NotifyCanExecuteChanged();
        OpenSettingsCommand.NotifyCanExecuteChanged();
        ExportDomainsCommand.NotifyCanExecuteChanged();
        OpenHtmlViewerCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedClusterChanged(ClusterBucketViewModel? value)
    {
        RefreshSenderRows();
    }

    partial void OnShowSendersFromAllMarkedClustersChanged(bool value)
    {
        RefreshSenderRows();
    }

    partial void OnSortSendersByDomainChanged(bool value)
    {
        RefreshSenderRows();
    }

    partial void OnSortClustersAlphabeticallyChanged(bool value)
    {
        RefreshFilterClusters();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFiltersAndSearch();
    }

    partial void OnShowOnlyMarkedClustersChanged(bool value)
    {
        RefreshFilterClusters();
        ApplyFiltersAndSearch();
    }

    partial void OnSelectedThemeModeChanged(string value)
    {
        ApplyThemeMode(value);
        SaveUiSettings();
    }

    partial void OnSelectedFilterClusterChanged(SelectableClusterViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        // Find the corresponding cluster in Clusters and select it
        var targetCluster = Clusters.FirstOrDefault(c => c.Cluster.Equals(value.Cluster, StringComparison.OrdinalIgnoreCase));
        if (targetCluster is not null)
        {
            SelectedCluster = targetCluster;
        }
    }

    public (bool EnableOutlook, bool EnableGmail, int MaxMessages, bool EnableNewsletterClustering,
        string ConfigPath, string ClientId, string GmailClientSecretsPath,
        string OutputPath, string JsonOutputPath, string HtmlOutputPath) GetCurrentSettings()
    {
        return (_enableOutlookOverride, _enableGmailOverride, _maxMessagesOverride, _enableNewsletterClusteringOverride,
            ConfigPath, _clientIdOverride, _gmailClientSecretsPathOverride,
            _outputPathOverride, _jsonOutputPathOverride, _htmlOutputPathOverride);
    }

    public void ApplySettings(bool enableOutlook, bool enableGmail, int maxMessages,
        bool enableNewsletterClustering, string newConfigPath,
        string clientId, string gmailClientSecretsPath,
        string outputPath, string jsonOutputPath, string htmlOutputPath)
    {
        _enableOutlookOverride = enableOutlook;
        _enableGmailOverride = enableGmail;
        _maxMessagesOverride = maxMessages;
        _enableNewsletterClusteringOverride = enableNewsletterClustering;
        ConfigPath = newConfigPath;
        _clientIdOverride = clientId;
        _gmailClientSecretsPathOverride = gmailClientSecretsPath;
        _outputPathOverride = outputPath;
        _jsonOutputPathOverride = jsonOutputPath;
        _htmlOutputPathOverride = htmlOutputPath;
        _lastHtmlOutputPath = htmlOutputPath;
        StatusMessage = "Settings updated.";
    }

    private void LoadSettingsFromConfig()
    {
        try
        {
            var args = BuildConfigArguments(ConfigPath);
            var settings = ConfigurationLoader.Load(args);
            _enableOutlookOverride = settings.EnableOutlook;
            _enableGmailOverride = settings.EnableGmail;
            _enableNewsletterClusteringOverride = settings.EnableNewsletterClustering;
            _maxMessagesOverride = settings.MaxMessages;
            _clientIdOverride = settings.ClientId;
            _gmailClientSecretsPathOverride = settings.GmailClientSecretsPath;
            _outputPathOverride = settings.OutputPath;
            _jsonOutputPathOverride = settings.JsonOutputPath;
            _htmlOutputPathOverride = settings.HtmlOutputPath;
            _lastHtmlOutputPath = settings.HtmlOutputPath;
            _lastLoadedSettings = settings;
        }
        catch
        {
            // keep defaults if config is missing or invalid
        }
    }

    private bool CanRun()
    {
        return !IsBusy;
    }

    private bool CanUseDialogCommands()
    {
        return !IsBusy;
    }

    private bool CanExportDomains()
    {
        return !IsBusy && MarkedClusterCount > 0;
    }

    private async Task OpenJsonAsync()
    {
        try
        {
            var report = await _dialogService.OpenReportJsonAsync();
            if (report is null)
            {
                return;
            }

            LoadReportFromJson(report);
        }
        catch (Exception exception)
        {
            StatusMessage = "Öffnen der JSON-Datei fehlgeschlagen";
            AppendLog(exception.Message);
        }
    }

    private async Task ExportDomainsAsync()
    {
        var domains = GetMarkedDomains();
        if (domains.Count == 0)
        {
            StatusMessage = "Keine markierten Domains zum Exportieren.";
            return;
        }

        try
        {
            var exported = await _dialogService.ExportDomainsAsync(domains);
            if (exported)
            {
                StatusMessage = $"{domains.Count} Domains exportiert.";
            }
        }
        catch (Exception exception)
        {
            StatusMessage = "Domain-Export fehlgeschlagen";
            AppendLog(exception.Message);
        }
    }

    private async Task OpenSettingsAsync()
    {
        var current = new MainWindowSettingsState
        {
            EnableOutlook = _enableOutlookOverride,
            EnableGmail = _enableGmailOverride,
            MaxMessages = _maxMessagesOverride,
            EnableNewsletterClustering = _enableNewsletterClusteringOverride,
            ConfigPath = ConfigPath,
            ClientId = _clientIdOverride,
            GmailClientSecretsPath = _gmailClientSecretsPathOverride,
            OutputPath = _outputPathOverride,
            JsonOutputPath = _jsonOutputPathOverride,
            HtmlOutputPath = _htmlOutputPathOverride
        };

        try
        {
            var updated = await _dialogService.OpenSettingsDialogAsync(current, _lastLoadedSettings);
            if (updated is null)
            {
                return;
            }

            ApplySettings(
                updated.EnableOutlook,
                updated.EnableGmail,
                updated.MaxMessages,
                updated.EnableNewsletterClustering,
                updated.ConfigPath,
                updated.ClientId,
                updated.GmailClientSecretsPath,
                updated.OutputPath,
                updated.JsonOutputPath,
                updated.HtmlOutputPath);
        }
        catch (Exception exception)
        {
            StatusMessage = "Öffnen der Einstellungen fehlgeschlagen";
            AppendLog(exception.Message);
        }
    }

    private async Task RunAsync()
    {
        IsBusy = true;
        StatusMessage = "Running mail analysis...";
        RunLog = string.Empty;

        try
        {
            var args = BuildConfigArguments(ConfigPath);
            var settings = ConfigurationLoader.Load(args);
            settings.Validate();

            settings.EnableOutlook = _enableOutlookOverride;
            settings.EnableGmail = _enableGmailOverride;
            settings.EnableNewsletterClustering = _enableNewsletterClusteringOverride;
            settings.MaxMessages = _maxMessagesOverride;
            settings.ClientId = _clientIdOverride;
            settings.GmailClientSecretsPath = _gmailClientSecretsPathOverride;
            settings.OutputPath = _outputPathOverride;
            settings.JsonOutputPath = _jsonOutputPathOverride;
            settings.HtmlOutputPath = _htmlOutputPathOverride;

            _lastHtmlOutputPath = settings.HtmlOutputPath;

            AppendLog($"Outlook enabled: {settings.EnableOutlook}");
            AppendLog($"Gmail enabled: {settings.EnableGmail}");
            AppendLog($"Max messages per provider: {settings.MaxMessages}");

            var result = await MailGrabberRunner.RunAsync(settings, AppendLog);
            LoadReport(result.Report);

            StatusMessage = $"Done. {result.TotalMessages} messages, {result.RowCount} sender rows.";
        }
        catch (Exception exception)
        {
            StatusMessage = "Run failed";
            AppendLog(exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadReport(ClusterReport report)
    {
        _fullReport = report;
        AllClusters.Clear();
        Clusters.Clear();
        SenderRows.Clear();

        foreach (var cluster in report.Clusters)
        {
            AllClusters.Add(new SelectableClusterViewModel(cluster, OnClusterSelectionChanged));
        }

        RefreshFilterClusters();
        ApplyFiltersAndSearch();
        SelectedCluster = Clusters.FirstOrDefault();
        SelectedFilterCluster = FilterClusters.FirstOrDefault();
    }

    private void OnClusterSelectionChanged()
    {
        UpdateMarkedClusterCount();
        
        // Only refresh the filter list if "Show only marked" is active
        if (ShowOnlyMarkedClusters)
        {
            RefreshFilterClusters();
        }
        
        ApplyFiltersAndSearch();

        // If showing senders from all marked clusters, refresh the sender list
        if (ShowSendersFromAllMarkedClusters)
        {
            RefreshSenderRows();
        }
    }

    private void UpdateMarkedClusterCount()
    {
        MarkedClusterCount = AllClusters.Count(c => c.IsSelected);
        ExportDomainsCommand.NotifyCanExecuteChanged();
    }

    private void RefreshFilterClusters()
    {
        var previous = SelectedFilterCluster?.Cluster;
        var items = ShowOnlyMarkedClusters
            ? AllClusters.Where(c => c.IsSelected).ToList()
            : AllClusters.ToList();

        // Apply sorting if enabled
        if (SortClustersAlphabetically)
        {
            items = items.OrderBy(c => c.Cluster, StringComparer.OrdinalIgnoreCase).ToList();
        }

        FilterClusters.Clear();
        foreach (var item in items)
        {
            FilterClusters.Add(item);
        }

        SelectedFilterCluster = FilterClusters.FirstOrDefault(c => c.Cluster.Equals(previous, StringComparison.OrdinalIgnoreCase))
            ?? FilterClusters.FirstOrDefault();
    }

    private void ApplyFiltersAndSearch()
    {
        if (_fullReport is null)
        {
            return;
        }

        var selectedClusterNames = AllClusters
            .Where(c => c.IsSelected)
            .Select(c => c.Cluster)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var search = SearchText?.Trim() ?? string.Empty;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Clusters.Clear();

        var clustersToShow = _fullReport.Clusters.Where(cluster =>
        {
            var clusterMatches = !ShowOnlyMarkedClusters || selectedClusterNames.Contains(cluster.Cluster);
            if (!clusterMatches)
            {
                return false;
            }

            if (!hasSearch)
            {
                return true;
            }

            return cluster.Cluster.Contains(search, StringComparison.OrdinalIgnoreCase)
                || cluster.SenderAddresses.Any(s =>
                    s.SenderAddress.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || s.Domain.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (s.SenderName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }).ToList();

        foreach (var cluster in clustersToShow)
        {
            Clusters.Add(new ClusterBucketViewModel(cluster));
        }

        if (SelectedCluster is null || !clustersToShow.Any(c => c.Cluster.Equals(SelectedCluster.Cluster, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedCluster = Clusters.FirstOrDefault();
        }
    }

    private void RefreshSenderRows()
    {
        SenderRows.Clear();

        List<SenderRowViewModel> allSenders = new();

        if (ShowSendersFromAllMarkedClusters)
        {
            // Show senders from all marked clusters
            var markedClusterNames = AllClusters
                .Where(c => c.IsSelected)
                .Select(c => c.Cluster)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (_fullReport is not null)
            {
                foreach (var cluster in _fullReport.Clusters)
                {
                    if (markedClusterNames.Contains(cluster.Cluster))
                    {
                        allSenders.AddRange(cluster.SenderAddresses.Select(s => new SenderRowViewModel(s)));
                    }
                }
            }
        }
        else
        {
            // Show senders only from selected cluster
            if (SelectedCluster is null)
            {
                return;
            }

            allSenders.AddRange(SelectedCluster.SenderAddresses);
        }

        // Apply search filter
        var search = SearchText?.Trim() ?? string.Empty;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        var senderRows = hasSearch
            ? allSenders.Where(s =>
                s.SenderAddress.Contains(search, StringComparison.OrdinalIgnoreCase)
                || s.Domain.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (s.SenderName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
            : allSenders;

        // Apply sorting if enabled
        if (SortSendersByDomain)
        {
            senderRows = senderRows.OrderBy(s => s.Domain, StringComparer.OrdinalIgnoreCase).ToList();
        }
        else
        {
            senderRows = senderRows.ToList();
        }

        foreach (var sender in senderRows)
        {
            SenderRows.Add(sender);
        }
    }

    private void RefreshSenderRows(ClusterBucketViewModel? cluster)
    {
        RefreshSenderRows();
    }

    private void ClearFilter()
    {
        SearchText = string.Empty;
        ShowOnlyMarkedClusters = false;

        foreach (var cluster in AllClusters)
        {
            cluster.IsSelected = false;
        }

        UpdateMarkedClusterCount();
        RefreshFilterClusters();
        ApplyFiltersAndSearch();
    }

    private void ToggleSelectedFilterCluster()
    {
        if (SelectedFilterCluster is null)
        {
            return;
        }

        SelectedFilterCluster.IsSelected = !SelectedFilterCluster.IsSelected;
    }

    private static void ApplyThemeMode(string? mode)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = mode switch
        {
            "Hell" => ThemeVariant.Light,
            "Dunkel" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void LoadUiSettings()
    {
        try
        {
            if (!File.Exists(UiSettingsPath))
            {
                return;
            }

            var json = File.ReadAllText(UiSettingsPath);
            var uiSettings = JsonSerializer.Deserialize<UiSettings>(json, UiJsonOptions);
            if (uiSettings is null)
            {
                return;
            }

            var loadedMode = NormalizeThemeMode(uiSettings.ThemeMode);
            if (!string.IsNullOrWhiteSpace(loadedMode))
            {
                SelectedThemeMode = loadedMode;
            }
        }
        catch
        {
            // Ignore corrupted UI settings and continue with defaults.
        }
    }

    private void SaveUiSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(UiSettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var payload = new UiSettings
            {
                ThemeMode = NormalizeThemeMode(SelectedThemeMode) ?? "System"
            };

            var json = JsonSerializer.Serialize(payload, UiJsonOptions);
            File.WriteAllText(UiSettingsPath, json);
        }
        catch
        {
            // Saving UI preferences is optional; ignore IO failures.
        }
    }

    private static string? NormalizeThemeMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return null;
        }

        return mode.Trim().ToLowerInvariant() switch
        {
            "hell" or "light" => "Hell",
            "dunkel" or "dark" => "Dunkel",
            "system" or "default" => "System",
            _ => null
        };
    }

    private sealed class UiSettings
    {
        public string ThemeMode { get; set; } = "System";
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        if (string.IsNullOrWhiteSpace(RunLog))
        {
            RunLog = line;
            return;
        }

        var builder = new StringBuilder(RunLog.Length + line.Length + Environment.NewLine.Length);
        builder.Append(RunLog);
        builder.AppendLine();
        builder.Append(line);
        RunLog = builder.ToString();
    }

    private bool CanOpenHtmlViewer()
    {
        return !IsBusy && File.Exists(Path.GetFullPath(_lastHtmlOutputPath));
    }

    private void OpenHtmlViewer()
    {
        var fullPath = Path.GetFullPath(_lastHtmlOutputPath);
        if (!File.Exists(fullPath))
        {
            StatusMessage = "HTML viewer file not found.";
            return;
        }

        OpenPath(fullPath);
    }

    private void OpenOutputFolder()
    {
        var fullPath = Path.GetFullPath(_lastHtmlOutputPath);
        var directory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        OpenPath(directory);
    }

    private static void OpenPath(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private static string[] BuildConfigArguments(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            return Array.Empty<string>();
        }

        return ["--config", configPath.Trim()];
    }
}

internal sealed class NullMainWindowDialogService : IMainWindowDialogService
{
    public Task<ClusterReport?> OpenReportJsonAsync() => Task.FromResult<ClusterReport?>(null);

    public Task<bool> ExportDomainsAsync(IReadOnlyList<string> domains) => Task.FromResult(false);

    public Task<MainWindowSettingsState?> OpenSettingsDialogAsync(MainWindowSettingsState current, AppSettings? baseSettings)
        => Task.FromResult<MainWindowSettingsState?>(null);
}

public partial class ClusterBucketViewModel : ObservableObject
{
    [ObservableProperty]
    private string cluster = string.Empty;

    [ObservableProperty]
    private bool isNewsletterCluster;

    [ObservableProperty]
    private int senderCount;

    [ObservableProperty]
    private int messageCount;

    [ObservableProperty]
    private List<SenderRowViewModel> senderAddresses = [];

    public ClusterBucketViewModel(ClusterBucket bucket)
    {
        Cluster = bucket.Cluster;
        IsNewsletterCluster = bucket.IsNewsletterCluster;
        SenderCount = bucket.SenderCount;
        MessageCount = bucket.MessageCount;
        SenderAddresses = bucket.SenderAddresses.Select(sender => new SenderRowViewModel(sender)).ToList();
    }

    public string Summary => $"{SenderCount} senders / {MessageCount} messages";
}

public partial class SelectableClusterViewModel : ObservableObject
{
    private readonly Action _onChanged;

    [ObservableProperty]
    private string cluster = string.Empty;

    [ObservableProperty]
    private int messageCount;

    [ObservableProperty]
    private int senderCount;

    [ObservableProperty]
    private bool isSelected;

    public SelectableClusterViewModel(ClusterBucket bucket, Action onChanged)
    {
        Cluster = bucket.Cluster;
        MessageCount = bucket.MessageCount;
        SenderCount = bucket.SenderCount;
        _onChanged = onChanged;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        _onChanged?.Invoke();
    }

    public string DisplayText => $"{Cluster} ({SenderCount} senders, {MessageCount} messages)";
}

public partial class SenderRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string senderAddress = string.Empty;

    [ObservableProperty]
    private string senderName = string.Empty;

    [ObservableProperty]
    private string domain = string.Empty;

    [ObservableProperty]
    private int messageCount;

    [ObservableProperty]
    private string providers = string.Empty;

    [ObservableProperty]
    private string accounts = string.Empty;

    [ObservableProperty]
    private string sampleSubjects = string.Empty;

    public SenderRowViewModel(ClusteredSenderRow row)
    {
        SenderAddress = row.SenderAddress;
        SenderName = row.SenderName;
        Domain = row.Domain;
        MessageCount = row.MessageCount;
        Providers = string.Join(", ", row.Providers);
        Accounts = string.Join(", ", row.SourceAccounts);
        SampleSubjects = row.SampleSubjects.Count == 0 ? "-" : string.Join(" | ", row.SampleSubjects);
    }
}
