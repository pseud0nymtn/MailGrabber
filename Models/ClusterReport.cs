namespace MailGrabber.Models;

public sealed class ClusterReport
{
    public DateTimeOffset GeneratedAtUtc { get; init; }

    public int TotalInputMessages { get; init; }

    public required List<ClusterBucket> Clusters { get; init; }
}

public sealed class ClusterBucket
{
    public required string Cluster { get; init; }

    public bool IsNewsletterCluster { get; init; }

    public int SenderCount { get; init; }

    public int MessageCount { get; init; }

    public required List<ClusteredSenderRow> SenderAddresses { get; init; }
}
