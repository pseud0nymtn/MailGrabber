using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MailGrabber.Models;

namespace MailGrabber.ViewModels;

public partial class SettingsDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private bool enableOutlook = true;

    [ObservableProperty]
    private bool enableGmail;

    [ObservableProperty]
    private bool enableNewsletterClustering = true;

    [ObservableProperty]
    private int maxMessages = 2000;

    [ObservableProperty]
    private string configPath = "appsettings.json";

    [ObservableProperty]
    private string outputPath = "output/sender-clusters.csv";

    [ObservableProperty]
    private string jsonOutputPath = "output/sender-clusters.json";

    [ObservableProperty]
    private string htmlOutputPath = "output/cluster-viewer.html";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsDialogViewModel()
    {
        OkCommand = new RelayCommand(() => OnOk?.Invoke());
        CancelCommand = new RelayCommand(() => OnCancel?.Invoke());
    }

    public IRelayCommand OkCommand { get; }

    public IRelayCommand CancelCommand { get; }

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
