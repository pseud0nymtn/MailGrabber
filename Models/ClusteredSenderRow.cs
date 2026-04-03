namespace MailGrabber.Models;

public sealed class ClusteredSenderRow
{
    public required string Cluster { get; init; }

    public required string Tld { get; init; }

    public required string Domain { get; init; }

    public required string SenderAddress { get; init; }

    public required string SenderName { get; init; }

    public required List<string> Providers { get; init; }

    public required List<string> SourceAccounts { get; init; }

    public int MessageCount { get; init; }

    public DateTimeOffset? FirstSeenUtc { get; init; }

    public DateTimeOffset? LastSeenUtc { get; init; }

    public bool IsNewsletter { get; init; }

    public required List<string> SampleSubjects { get; init; }
}