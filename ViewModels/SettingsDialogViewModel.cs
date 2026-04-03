using System.Text.Json;
using System.Windows.Input;
using MailGrabber.Infrastructure;
using MailGrabber.Models;

namespace MailGrabber.ViewModels;

public sealed class SettingsDialogViewModel : ViewModelBase
{
    private bool _enableOutlook = true;
    private bool _enableGmail = false;
    private bool _enableNewsletterClustering = true;
    private int _maxMessages = 2000;
    private string _configPath = "appsettings.json";
    private string _outputPath = "output/sender-clusters.csv";
    private string _jsonOutputPath = "output/sender-clusters.json";
    private string _htmlOutputPath = "output/cluster-viewer.html";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly RelayCommand _okCommand;
    private readonly RelayCommand _cancelCommand;

    public SettingsDialogViewModel()
    {
        _okCommand = new RelayCommand(() => OnOk?.Invoke());
        _cancelCommand = new RelayCommand(() => OnCancel?.Invoke());
    }

    public bool EnableOutlook
    {
        get => _enableOutlook;
        set => SetProperty(ref _enableOutlook, value);
    }

    public bool EnableGmail
    {
        get => _enableGmail;
        set => SetProperty(ref _enableGmail, value);
    }

    public bool EnableNewsletterClustering
    {
        get => _enableNewsletterClustering;
        set => SetProperty(ref _enableNewsletterClustering, value);
    }

    public int MaxMessages
    {
        get => _maxMessages;
        set => SetProperty(ref _maxMessages, value);
    }

    public string ConfigPath
    {
        get => _configPath;
        set => SetProperty(ref _configPath, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public string JsonOutputPath
    {
        get => _jsonOutputPath;
        set => SetProperty(ref _jsonOutputPath, value);
    }

    public string HtmlOutputPath
    {
        get => _htmlOutputPath;
        set => SetProperty(ref _htmlOutputPath, value);
    }

    public ICommand OkCommand => _okCommand;

    public ICommand CancelCommand => _cancelCommand;

    public Action? OnOk { get; set; }

    public Action? OnCancel { get; set; }

    /// <summary>The full AppSettings loaded from disk, used as a base when saving.</summary>
    public AppSettings? BaseSettings { get; set; }

    public void SaveToPath(string path)
    {
        var settings = BaseSettings != null
            ? JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(BaseSettings, JsonOptions), JsonOptions) ?? new AppSettings()
            : new AppSettings();

        settings.EnableOutlook = EnableOutlook;
        settings.EnableGmail = EnableGmail;
        settings.EnableNewsletterClustering = EnableNewsletterClustering;
        settings.MaxMessages = MaxMessages;
        settings.OutputPath = OutputPath;
        settings.JsonOutputPath = JsonOutputPath;
        settings.HtmlOutputPath = HtmlOutputPath;

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
