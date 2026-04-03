using System.Windows.Input;
using MailGrabber.Infrastructure;

namespace MailGrabber.ViewModels;

public sealed class SettingsDialogViewModel : ViewModelBase
{
    private bool _enableOutlook = true;
    private bool _enableGmail = false;
    private int _maxMessages = 2000;
    private string _outputPath = "output/sender-clusters.csv";
    private string _jsonOutputPath = "output/sender-clusters.json";
    private string _htmlOutputPath = "output/cluster-viewer.html";

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

    public int MaxMessages
    {
        get => _maxMessages;
        set => SetProperty(ref _maxMessages, value);
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
}
