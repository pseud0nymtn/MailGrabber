using System.Text.Json;
using MailGrabber.Models;
using MailGrabber.Services;

namespace MailGrabber.Tests;

public class ExporterTests
{
    [Test]
    public void CsvExporter_WritesHeaderAndEscapedContent()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var path = Path.Combine(tempDir, "out", "rows.csv");
            var rows = new List<ClusteredSenderRow>
            {
                new()
                {
                    Cluster = "newsletter",
                    Tld = "com",
                    Domain = "example.com",
                    SenderAddress = "sender@example.com",
                    SenderName = "Name, \"Quoted\"",
                    Providers = ["gmail", "outlook"],
                    SourceAccounts = ["work"],
                    MessageCount = 3,
                    FirstSeenUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                    LastSeenUtc = DateTimeOffset.Parse("2026-01-02T00:00:00Z"),
                    IsNewsletter = true,
                    SampleSubjects = ["Hello", "Line\nBreak"]
                }
            };

            CsvExporter.Write(path, rows);

            var content = File.ReadAllText(path);
            Assert.Multiple(() =>
            {
                Assert.That(content, Does.Contain("Cluster,Tld,Domain"));
                Assert.That(content, Does.Contain("\"Name, \"\"Quoted\"\"\""));
                Assert.That(content, Does.Contain("true"));
                Assert.That(content, Does.Contain("gmail | outlook"));
            });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void JsonExporter_WritesIndentedJson()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var path = Path.Combine(tempDir, "out", "report.json");
            var report = BuildReport();

            JsonExporter.Write(path, report);

            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<ClusterReport>(json);

            Assert.Multiple(() =>
            {
                Assert.That(json, Does.Contain("\n"));
                Assert.That(loaded?.Clusters.Count, Is.EqualTo(1));
                Assert.That(loaded?.TotalInputMessages, Is.EqualTo(1));
            });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void HtmlViewerExporter_WritesHtmlAndEmbedsData()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var path = Path.Combine(tempDir, "out", "viewer.html");
            var report = BuildReport();

            HtmlViewerExporter.Write(path, report);

            var html = File.ReadAllText(path);
            Assert.Multiple(() =>
            {
                Assert.That(html, Does.Contain("<!doctype html>"));
                Assert.That(html, Does.Contain("MailGrabber Cluster Viewer"));
                Assert.That(html, Does.Contain("clusterEntries"));
                Assert.That(html, Does.Contain("example.com"));
            });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static ClusterReport BuildReport()
    {
        return new ClusterReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            TotalInputMessages = 1,
            Clusters =
            [
                new ClusterBucket
                {
                    Cluster = "example.com",
                    IsNewsletterCluster = false,
                    SenderCount = 1,
                    MessageCount = 1,
                    SenderAddresses =
                    [
                        new ClusteredSenderRow
                        {
                            Cluster = "example.com",
                            Tld = "com",
                            Domain = "example.com",
                            SenderAddress = "sender@example.com",
                            SenderName = "Sender",
                            Providers = ["gmail"],
                            SourceAccounts = ["acc"],
                            MessageCount = 1,
                            SampleSubjects = ["subject"]
                        }
                    ]
                }
            ]
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "mailgrabber-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
