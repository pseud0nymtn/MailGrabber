using MailGrabber.Models;

namespace MailGrabber.Services;

public static class ReportBuilder
{
    public static ClusterReport Build(List<ClusteredSenderRow> rows, int totalInputMessages)
    {
        var clusters = rows
            .GroupBy(row => row.Cluster, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var senderAddresses = group
                    .OrderByDescending(item => item.MessageCount)
                    .ThenBy(item => item.SenderAddress, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new ClusterBucket
                {
                    Cluster = group.Key,
                    IsNewsletterCluster = senderAddresses.Any(item => item.IsNewsletter),
                    SenderCount = senderAddresses.Count,
                    MessageCount = senderAddresses.Sum(item => item.MessageCount),
                    SenderAddresses = senderAddresses
                };
            })
            .OrderByDescending(cluster => cluster.MessageCount)
            .ThenBy(cluster => cluster.Cluster, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ClusterReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            TotalInputMessages = totalInputMessages,
            Clusters = clusters
        };
    }
}
