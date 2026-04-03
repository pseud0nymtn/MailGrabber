using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using MailGrabber.Infrastructure;
using MailGrabber.Models;
using MailGrabber.Services;

namespace MailGrabber.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly AsyncCommand _runCommand;
    private readonly RelayCommand _openHtmlViewerCommand;
    private readonly RelayCommand _openOutputFolderCommand;
    private readonly RelayCommand _clearFilterCommand;

    private string _configPath = "appsettings.json";
    private string _statusMessage = "Ready";
    private string _runLog = string.Empty;
    private bool _isBusy;
    private ClusterBucketViewModel? _selectedCluster;
    private string _lastHtmlOutputPath = "output/cluster-viewer.html";
    private string _searchText = string.Empty;
    private ClusterReport? _fullReport;
    private bool _enableOutlookOverride = true;
    private bool _enableGmailOverride = false;
    private bool _enableNewsletterClusteringOverride = true;
    private int _maxMessagesOverride = 2000;
    private string _outputPathOverride = "output/sender-clusters.csv";
    private string _jsonOutputPathOverride = "output/sender-clusters.json";
    private string _htmlOutputPathOverride = "output/cluster-viewer.html";
    private AppSettings? _lastLoadedSettings;

    public MainWindowViewModel()
    {
        Clusters = new ObservableCollection<ClusterBucketViewModel>();
        AllClusters = new ObservableCollection<SelectableClusterViewModel>();
        SenderRows = new ObservableCollection<SenderRowViewModel>();

        _runCommand = new AsyncCommand(RunAsync, () => !IsBusy);
        _openHtmlViewerCommand = new RelayCommand(OpenHtmlViewer, CanOpenHtmlViewer);
        _openOutputFolderCommand = new RelayCommand(OpenOutputFolder);
        _clearFilterCommand = new RelayCommand(ClearFilter);

        RunCommand = _runCommand;
        OpenHtmlViewerCommand = _openHtmlViewerCommand;
        OpenOutputFolderCommand = _openOutputFolderCommand;
        ClearFilterCommand = _clearFilterCommand;

        LoadSettingsFromConfig();
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

        public (bool EnableOutlook, bool EnableGmail, int MaxMessages, bool EnableNewsletterClustering,
            string ConfigPath, string OutputPath, string JsonOutputPath, string HtmlOutputPath) GetCurrentSettings()
        {
        return (_enableOutlookOverride, _enableGmailOverride, _maxMessagesOverride, _enableNewsletterClusteringOverride,
            _configPath, _outputPathOverride, _jsonOutputPathOverride, _htmlOutputPathOverride);
        }

        public AppSettings? LastLoadedSettings => _lastLoadedSettings;

    public string ConfigPath
    {
        get => _configPath;
        set => SetProperty(ref _configPath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string RunLog
    {
        get => _runLog;
        private set => SetProperty(ref _runLog, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            _runCommand.RaiseCanExecuteChanged();
            _openHtmlViewerCommand.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<ClusterBucketViewModel> Clusters { get; }

    public ObservableCollection<SelectableClusterViewModel> AllClusters { get; }

    public ObservableCollection<SenderRowViewModel> SenderRows { get; }

    public ClusterBucketViewModel? SelectedCluster
    {
        get => _selectedCluster;
        set
        {
            if (!SetProperty(ref _selectedCluster, value))
            {
                return;
            }

            RefreshSenderRows(value);
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
            {
                return;
            }

            ApplyFiltersAndSearch();
        }
    }

    public ICommand RunCommand { get; }

    public ICommand OpenHtmlViewerCommand { get; }

    public ICommand OpenOutputFolderCommand { get; }

    public ICommand ClearFilterCommand { get; }

    public void ApplySettings(bool enableOutlook, bool enableGmail, int maxMessages,
        bool enableNewsletterClustering, string configPath,
        string outputPath, string jsonOutputPath, string htmlOutputPath)
    {
        _enableOutlookOverride = enableOutlook;
        _enableGmailOverride = enableGmail;
        _maxMessagesOverride = maxMessages;
        _enableNewsletterClusteringOverride = enableNewsletterClustering;
        ConfigPath = configPath;
        _outputPathOverride = outputPath;
        _jsonOutputPathOverride = jsonOutputPath;
        _htmlOutputPathOverride = htmlOutputPath;
        _lastHtmlOutputPath = htmlOutputPath;
        StatusMessage = "Settings updated.";
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
            _openHtmlViewerCommand.RaiseCanExecuteChanged();
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

        ApplyFiltersAndSearch();
        SelectedCluster = Clusters.FirstOrDefault();
    }

    private void OnClusterSelectionChanged()
    {
        ApplyFiltersAndSearch();
    }

    private void ApplyFiltersAndSearch()
    {
        var selectedClusterNames = AllClusters
            .Where(c => c.IsSelected)
            .Select(c => c.Cluster)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var search = SearchText?.Trim() ?? string.Empty;
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var hasClusterFilter = selectedClusterNames.Count > 0 && selectedClusterNames.Count < AllClusters.Count;

        if (_fullReport is null)
        {
            return;
        }

        Clusters.Clear();

        var clustersToShow = _fullReport.Clusters.Where(cluster =>
        {
            var clusterMatches = !hasClusterFilter || selectedClusterNames.Contains(cluster.Cluster);
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
        foreach (var cluster in AllClusters)
        {
            cluster.IsSelected = false;
        }
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
        if (IsBusy)
        {
            return false;
        }

        return File.Exists(Path.GetFullPath(_lastHtmlOutputPath));
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

public sealed class ClusterBucketViewModel
{
    public ClusterBucketViewModel(ClusterBucket bucket)
    {
        Cluster = bucket.Cluster;
        IsNewsletterCluster = bucket.IsNewsletterCluster;
        SenderCount = bucket.SenderCount;
        MessageCount = bucket.MessageCount;
        SenderAddresses = bucket.SenderAddresses.Select(sender => new SenderRowViewModel(sender)).ToList();
    }

    public string Cluster { get; }

    public bool IsNewsletterCluster { get; }

    public int SenderCount { get; }

    public int MessageCount { get; }

    public List<SenderRowViewModel> SenderAddresses { get; }

    public string Summary => $"{SenderCount} senders / {MessageCount} messages";
}

public sealed class SelectableClusterViewModel : ViewModelBase
{
    private bool _isSelected;
    private readonly Action _onChanged;

    public SelectableClusterViewModel(ClusterBucket bucket, Action onChanged)
    {
        Cluster = bucket.Cluster;
        MessageCount = bucket.MessageCount;
        SenderCount = bucket.SenderCount;
        _onChanged = onChanged;
    }

    public string Cluster { get; }

    public int MessageCount { get; }

    public int SenderCount { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value))
            {
                return;
            }

            _onChanged?.Invoke();
        }
    }

    public string DisplayText => $"{Cluster} ({SenderCount} senders, {MessageCount} messages)";
}

public sealed class SenderRowViewModel
{
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

    public string SenderAddress { get; }

    public string SenderName { get; }

    public string Domain { get; }

    public int MessageCount { get; }

    public string Providers { get; }

    public string Accounts { get; }

    public string SampleSubjects { get; }
}
