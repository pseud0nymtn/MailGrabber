using MailGrabber.Models;
using MailGrabber.Services;

namespace MailGrabber.Tests;

public class ClusteringAndReportTests
{
    [Test]
    public void SenderClusterer_BuildRows_ClustersAndAggregatesMessages()
    {
        var settings = new AppSettings
        {
            EnableOutlook = true,
            EnableGmail = false,
            ClientId = "x",
            EnableNewsletterClustering = true,
            NewsletterClusterName = "newsletter",
            NewsletterMailboxHints = ["news"],
            NewsletterDomainHints = ["mailing"]
        };

        var messages = new List<MailboxMessage>
        {
            new()
            {
                Provider = "gmail",
                AccountLabel = "a",
                SenderAddress = " News@sub.example.com ",
                SenderName = "News Team",
                Subject = "Subject A",
                ReceivedDateTime = DateTimeOffset.Parse("2026-02-01T10:00:00Z")
            },
            new()
            {
                Provider = "outlook",
                AccountLabel = "b",
                SenderAddress = "news@sub.example.com",
                SenderName = "News Team",
                Subject = "Subject B",
                ReceivedDateTime = DateTimeOffset.Parse("2026-02-01T11:00:00Z")
            },
            new()
            {
                Provider = "outlook",
                AccountLabel = "b",
                SenderAddress = "user@company.org",
                SenderName = "User",
                Subject = "Subject C",
                ReceivedDateTime = DateTimeOffset.Parse("2026-02-03T11:00:00Z")
            },
            new()
            {
                Provider = "outlook",
                AccountLabel = "b",
                SenderAddress = "invalid-address",
                SenderName = "Ignore",
                Subject = "Ignored"
            }
        };

        var rows = SenderClusterer.BuildRows(messages, settings);
        var newsletterRow = rows.Single(r => r.SenderAddress == "news@sub.example.com");
        var companyRow = rows.Single(r => r.SenderAddress == "user@company.org");

        Assert.Multiple(() =>
        {
            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(newsletterRow.IsNewsletter, Is.True);
            Assert.That(newsletterRow.Cluster, Is.EqualTo("newsletter"));
            Assert.That(newsletterRow.MessageCount, Is.EqualTo(2));
            Assert.That(newsletterRow.FirstSeenUtc, Is.EqualTo(DateTimeOffset.Parse("2026-02-01T10:00:00Z")));
            Assert.That(newsletterRow.LastSeenUtc, Is.EqualTo(DateTimeOffset.Parse("2026-02-01T11:00:00Z")));
            Assert.That(newsletterRow.Providers, Is.EqualTo(new[] { "gmail", "outlook" }));
            Assert.That(newsletterRow.SourceAccounts, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(companyRow.Cluster, Is.EqualTo("company.org"));
        });
    }

    [Test]
    public void ReportBuilder_Build_GroupsAndSortsClusters()
    {
        var rows = new List<ClusteredSenderRow>
        {
            new() { Cluster = "b", SenderAddress = "x@b.com", MessageCount = 1, IsNewsletter = false },
            new() { Cluster = "a", SenderAddress = "x@a.com", MessageCount = 4, IsNewsletter = true },
            new() { Cluster = "a", SenderAddress = "y@a.com", MessageCount = 2, IsNewsletter = false }
        };

        var report = ReportBuilder.Build(rows, 99);

        Assert.Multiple(() =>
        {
            Assert.That(report.TotalInputMessages, Is.EqualTo(99));
            Assert.That(report.Clusters.Count, Is.EqualTo(2));
            Assert.That(report.Clusters[0].Cluster, Is.EqualTo("a"));
            Assert.That(report.Clusters[0].MessageCount, Is.EqualTo(6));
            Assert.That(report.Clusters[0].SenderCount, Is.EqualTo(2));
            Assert.That(report.Clusters[0].IsNewsletterCluster, Is.True);
            Assert.That(report.GeneratedAtUtc, Is.Not.EqualTo(default(DateTimeOffset)));
        });
    }

    [Test]
    public void PublicSuffixHelper_HandlesNullAndFallbacks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PublicSuffixHelper.GetRegistrableDomain(null), Is.Null);
            Assert.That(PublicSuffixHelper.GetTld(""), Is.Null);
            Assert.That(PublicSuffixHelper.GetRegistrableDomain("sub.example.co.uk"), Is.EqualTo("example.co.uk"));
            Assert.That(PublicSuffixHelper.GetTld("sub.example.co.uk"), Is.EqualTo("co.uk"));
            Assert.That(PublicSuffixHelper.GetTld("localhost"), Is.Null);
        });
    }
}
