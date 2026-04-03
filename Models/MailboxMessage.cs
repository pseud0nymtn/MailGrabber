using CommunityToolkit.Mvvm.ComponentModel;

namespace MailGrabber.Models;

public partial class MailboxMessage : ObservableObject
{
    [ObservableProperty]
    private string provider = string.Empty;

    [ObservableProperty]
    private string accountLabel = string.Empty;

    [ObservableProperty]
    private string? senderAddress;

    [ObservableProperty]
    private string? senderName;

    [ObservableProperty]
    private string? subject;

    [ObservableProperty]
    private DateTimeOffset? receivedDateTime;
}
