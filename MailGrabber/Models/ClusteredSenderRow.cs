using CommunityToolkit.Mvvm.ComponentModel;

namespace MailGrabber.Models;

public partial class ClusteredSenderRow : ObservableObject
{
    [ObservableProperty]
    private string cluster = string.Empty;

    [ObservableProperty]
    private string tld = string.Empty;

    [ObservableProperty]
    private string domain = string.Empty;

    [ObservableProperty]
    private string senderAddress = string.Empty;

    [ObservableProperty]
    private string senderName = string.Empty;

    [ObservableProperty]
    private List<string> providers = [];

    [ObservableProperty]
    private List<string> sourceAccounts = [];

    [ObservableProperty]
    private int messageCount;

    [ObservableProperty]
    private DateTimeOffset? firstSeenUtc;

    [ObservableProperty]
    private DateTimeOffset? lastSeenUtc;

    [ObservableProperty]
    private bool isNewsletter;

    [ObservableProperty]
    private List<string> sampleSubjects = [];
}
