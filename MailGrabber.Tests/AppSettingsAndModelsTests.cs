using MailGrabber.Models;

namespace MailGrabber.Tests;

public class AppSettingsAndModelsTests
{
    [Test]
    public void GraphScopes_ReturnsExpectedScopes()
    {
        var settings = new AppSettings();

        Assert.That(settings.GraphScopes, Is.EqualTo(new[] { "Mail.Read", "User.Read" }));
    }

    [Test]
    public void Validate_Throws_WhenNoProviderEnabled()
    {
        var settings = new AppSettings
        {
            EnableOutlook = false,
            EnableGmail = false,
            ClientId = "client-id"
        };

        var action = () => settings.Validate();

        Assert.That(action, Throws.InvalidOperationException.With.Message.Contains("At least one provider"));
    }

    [Test]
    public void Validate_Throws_WhenOutlookEnabledWithPlaceholderClientId()
    {
        var settings = new AppSettings
        {
            EnableOutlook = true,
            EnableGmail = false,
            ClientId = "YOUR-CLIENT-ID-HERE"
        };

        var action = () => settings.Validate();

        Assert.That(action, Throws.InvalidOperationException.With.Message.Contains("No Microsoft app client ID"));
    }

    [Test]
    public void Validate_Throws_WhenGmailEnabledWithoutSecretsPath()
    {
        var settings = new AppSettings
        {
            EnableOutlook = false,
            EnableGmail = true,
            GmailClientSecretsPath = "   "
        };

        var action = () => settings.Validate();

        Assert.That(action, Throws.InvalidOperationException.With.Message.Contains("GmailClientSecretsPath"));
    }

    [Test]
    public void Validate_Throws_WhenMaxMessagesIsNotPositive()
    {
        var settings = new AppSettings
        {
            EnableOutlook = true,
            EnableGmail = false,
            ClientId = "valid-client",
            MaxMessages = 0
        };

        var action = () => settings.Validate();

        Assert.That(action, Throws.InvalidOperationException.With.Message.Contains("MaxMessages"));
    }

    [TestCase(0)]
    [TestCase(1001)]
    public void Validate_Throws_WhenPageSizeOutsideRange(int pageSize)
    {
        var settings = new AppSettings
        {
            EnableOutlook = true,
            EnableGmail = false,
            ClientId = "valid-client",
            MaxMessages = 1,
            PageSize = pageSize
        };

        var action = () => settings.Validate();

        Assert.That(action, Throws.InvalidOperationException.With.Message.Contains("PageSize"));
    }

    [Test]
    public void Validate_Throws_WhenOldestMessageAgeDaysIsNegative()
    {
        var settings = new AppSettings
        {
            EnableOutlook = true,
            EnableGmail = false,
            ClientId = "valid-client",
            MaxMessages = 1,
            PageSize = 50,
            OldestMessageAgeDays = -1
        };

        var action = () => settings.Validate();

        Assert.That(action, Throws.InvalidOperationException.With.Message.Contains("OldestMessageAgeDays"));
    }

    [Test]
    public void Validate_DoesNotThrow_ForValidSettings()
    {
        var settings = new AppSettings
        {
            EnableOutlook = true,
            EnableGmail = true,
            ClientId = "valid-client",
            GmailClientSecretsPath = "secret.json",
            MaxMessages = 100,
            PageSize = 50
        };

        Assert.That(() => settings.Validate(), Throws.Nothing);
    }

    [Test]
    public void ModelProperties_CanBeAssignedAndRead()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var row = new ClusteredSenderRow
        {
            Cluster = "example.com",
            Tld = "com",
            Domain = "example.com",
            SenderAddress = "sender@example.com",
            SenderName = "Sender",
            Providers = ["gmail"],
            SourceAccounts = ["personal"],
            MessageCount = 2,
            FirstSeenUtc = timestamp.AddDays(-1),
            LastSeenUtc = timestamp,
            IsNewsletter = true,
            SampleSubjects = ["hello"]
        };

        var bucket = new ClusterBucket
        {
            Cluster = "example.com",
            IsNewsletterCluster = true,
            SenderCount = 1,
            MessageCount = 2,
            SenderAddresses = [row]
        };

        var report = new ClusterReport
        {
            GeneratedAtUtc = timestamp,
            TotalInputMessages = 2,
            Clusters = [bucket]
        };

        var graph = new GraphMessagePage
        {
            Value =
            [
                new GraphMessageItem
                {
                    Subject = "subject",
                    ReceivedDateTime = timestamp,
                    From = new GraphRecipient { EmailAddress = new GraphEmailAddress { Address = "from@example.com", Name = "From" } },
                    Sender = new GraphRecipient { EmailAddress = new GraphEmailAddress { Address = "sender@example.com", Name = "Sender" } }
                }
            ],
            NextLink = "next"
        };

        var mailboxMessage = new MailboxMessage
        {
            Provider = "outlook",
            AccountLabel = "work",
            SenderAddress = "sender@example.com",
            SenderName = "Sender",
            Subject = "subject",
            ReceivedDateTime = timestamp
        };

        Assert.Multiple(() =>
        {
            Assert.That(report.Clusters.Single().Cluster, Is.EqualTo("example.com"));
            Assert.That(graph.Value.Single().Sender?.EmailAddress?.Address, Is.EqualTo("sender@example.com"));
            Assert.That(mailboxMessage.Provider, Is.EqualTo("outlook"));
            Assert.That(row.SampleSubjects.Single(), Is.EqualTo("hello"));
        });
    }
}
