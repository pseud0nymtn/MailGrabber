namespace MailGrabber.Models;

public sealed class MailboxMessage
{
    public required string Provider { get; init; }

    public required string AccountLabel { get; init; }

    public string? SenderAddress { get; init; }

    public string? SenderName { get; init; }

    public string? Subject { get; init; }

    public DateTimeOffset? ReceivedDateTime { get; init; }
}
