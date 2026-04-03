using CommunityToolkit.Mvvm.ComponentModel;

namespace MailGrabber.Models;

public partial class ClusterReport : ObservableObject
{
    [ObservableProperty]
    private DateTimeOffset generatedAtUtc;

    [ObservableProperty]
    private List<ClusterBucket> clusters = [];

    [ObservableProperty]
    private int totalInputMessages;
}

public partial class ClusterBucket : ObservableObject
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
    private List<ClusteredSenderRow> senderAddresses = [];
}
