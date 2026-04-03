using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MailGrabber.Models;
using MailGrabber.Services;

namespace MailGrabber.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private string _lastHtmlOutputPath = "output/cluster-viewer.html";
    private ClusterReport? _fullReport;
    private bool _enableOutlookOverride = true;
    private bool _enableGmailOverride;
    private bool _enableNewsletterClusteringOverride = true;
    private int _maxMessagesOverride = 2000;
    private string _outputPathOverride = "output/sender-clusters.csv";
    private string _jsonOutputPathOverride = "output/sender-clusters.json";
    private string _htmlOutputPathOverride = "output/cluster-viewer.html";
    private AppSettings? _lastLoadedSettings;

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

    public MainWindowViewModel()
    {
        Clusters = new ObservableCollection<ClusterBucketViewModel>();
        AllClusters = new ObservableCollection<SelectableClusterViewModel>();
        FilterClusters = new ObservableCollection<SelectableClusterViewModel>();
        SenderRows = new ObservableCollection<SenderRowViewModel>();

        RunCommand = new AsyncRelayCommand(RunAsync, CanRun);
        OpenHtmlViewerCommand = new RelayCommand(OpenHtmlViewer, CanOpenHtmlViewer);
        OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder);
        ClearFilterCommand = new RelayCommand(ClearFilter);

        LoadSettingsFromConfig();
    }

    public ObservableCollection<ClusterBucketViewModel> Clusters { get; }

    public ObservableCollection<SelectableClusterViewModel> AllClusters { get; }

    public ObservableCollection<SelectableClusterViewModel> FilterClusters { get; }

    public ObservableCollection<SenderRowViewModel> SenderRows { get; }

    public IAsyncRelayCommand RunCommand { get; }

    public IRelayCommand OpenHtmlViewerCommand { get; }

    public IRelayCommand OpenOutputFolderCommand { get; }

    public IRelayCommand ClearFilterCommand { get; }

    public AppSettings? LastLoadedSettings => _lastLoadedSettings;

    partial void OnIsBusyChanged(bool value)
    {
        RunCommand.NotifyCanExecuteChanged();
        OpenHtmlViewerCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedClusterChanged(ClusterBucketViewModel? value)
    {
        RefreshSenderRows(value);
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
        string ConfigPath, string OutputPath, string JsonOutputPath, string HtmlOutputPath) GetCurrentSettings()
    {
        return (_enableOutlookOverride, _enableGmailOverride, _maxMessagesOverride, _enableNewsletterClusteringOverride,
            ConfigPath, _outputPathOverride, _jsonOutputPathOverride, _htmlOutputPathOverride);
    }

    public void ApplySettings(bool enableOutlook, bool enableGmail, int maxMessages,
        bool enableNewsletterClustering, string newConfigPath,
        string outputPath, string jsonOutputPath, string htmlOutputPath)
    {
        _enableOutlookOverride = enableOutlook;
        _enableGmailOverride = enableGmail;
        _maxMessagesOverride = maxMessages;
        _enableNewsletterClusteringOverride = enableNewsletterClustering;
        ConfigPath = newConfigPath;
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
    }

    private void UpdateMarkedClusterCount()
    {
        MarkedClusterCount = AllClusters.Count(c => c.IsSelected);
    }

    private void RefreshFilterClusters()
    {
        var previous = SelectedFilterCluster?.Cluster;
        var items = ShowOnlyMarkedClusters
            ? AllClusters.Where(c => c.IsSelected).ToList()
            : AllClusters.ToList();

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

    private void RefreshSenderRows(ClusterBucketViewModel? cluster)
    {
        SenderRows.Clear();
        if (cluster is null)
        {
            return;
        }

        var search = SearchText?.Trim() ?? string.Empty;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        var senderRows = hasSearch
            ? cluster.SenderAddresses.Where(s =>
                s.SenderAddress.Contains(search, StringComparison.OrdinalIgnoreCase)
                || s.Domain.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (s.SenderName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
            : cluster.SenderAddresses;

        foreach (var sender in senderRows)
        {
            SenderRows.Add(sender);
        }
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
