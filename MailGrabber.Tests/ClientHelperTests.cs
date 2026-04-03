using System.Globalization;
using System.Reflection;
using Google.Apis.Gmail.v1.Data;
using MailGrabber.Models;
using MailGrabber.Services;

namespace MailGrabber.Tests;

public class ClientHelperTests
{
    [Test]
    public void GmailClient_ParseFromHeader_ParsesMailboxOrFallback()
    {
        var method = typeof(GmailClient).GetMethod("ParseFromHeader", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var parsed = ((string? SenderAddress, string? SenderName))method!.Invoke(null, ["Sender Name <sender@example.com>"])!;
        var fallbackAddress = ((string? SenderAddress, string? SenderName))method.Invoke(null, ["sender@example.com"])!;
        var fallbackName = ((string? SenderAddress, string? SenderName))method.Invoke(null, ["Only Name"] )!;
        var empty = ((string? SenderAddress, string? SenderName))method.Invoke(null, [null])!;

        Assert.Multiple(() =>
        {
            Assert.That(parsed.SenderAddress, Is.EqualTo("sender@example.com"));
            Assert.That(parsed.SenderName, Is.EqualTo("Sender Name"));
            Assert.That(fallbackAddress.SenderAddress, Is.EqualTo("sender@example.com"));
            Assert.That(fallbackAddress.SenderName, Is.EqualTo(string.Empty));
            Assert.That(fallbackName.SenderAddress, Is.Null);
            Assert.That(fallbackName.SenderName, Is.EqualTo("Only Name"));
            Assert.That(empty.SenderAddress, Is.Null);
            Assert.That(empty.SenderName, Is.Null);
        });
    }

    [Test]
    public void GmailClient_ParseReceivedDate_PrefersInternalDateThenHeader()
    {
        var method = typeof(GmailClient).GetMethod("ParseReceivedDate", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var millis = DateTimeOffset.Parse("2026-03-01T12:00:00Z", CultureInfo.InvariantCulture).ToUnixTimeMilliseconds();
        var messageWithInternalDate = new Message { InternalDate = millis };
        var messageWithDateHeader = new Message();
        var dateHeaders = new[] { new MessagePartHeader { Name = "Date", Value = "Mon, 02 Mar 2026 10:00:00 +0000" } };

        var fromMillis = (DateTimeOffset?)method!.Invoke(null, [messageWithInternalDate, Array.Empty<MessagePartHeader>()]);
        var fromHeader = (DateTimeOffset?)method.Invoke(null, [messageWithDateHeader, dateHeaders]);
        var none = (DateTimeOffset?)method.Invoke(null, [messageWithDateHeader, Array.Empty<MessagePartHeader>()]);

        Assert.Multiple(() =>
        {
            Assert.That(fromMillis, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(millis)));
            Assert.That(fromHeader, Is.Not.Null);
            Assert.That(none, Is.Null);
        });
    }

    [Test]
    public void GmailClient_GetHeader_IsCaseInsensitive()
    {
        var method = typeof(GmailClient).GetMethod("GetHeader", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var headers = new[]
        {
            new MessagePartHeader { Name = "Subject", Value = "Hello" }
        };

        var value = (string?)method!.Invoke(null, [headers, "subject"]);

        Assert.That(value, Is.EqualTo("Hello"));
    }

    [Test]
    public void GmailClient_ResolveTokenDirectory_UsesExplicitOrDefault()
    {
        var explicitClient = new GmailClient(new AppSettings { GmailTokenDirectory = "tokens-path" });
        var defaultClient = new GmailClient(new AppSettings { GmailTokenDirectory = "" });

        var method = typeof(GmailClient).GetMethod("ResolveTokenDirectory", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null);

        var explicitDir = (string)method!.Invoke(explicitClient, null)!;
        var defaultDir = (string)method.Invoke(defaultClient, null)!;

        Assert.Multiple(() =>
        {
            Assert.That(explicitDir, Is.EqualTo(Path.GetFullPath("tokens-path")));
            Assert.That(defaultDir, Does.Contain(Path.Combine("MailGrabber", "gmail-token")));
        });
    }

    [Test]
    public void GmailClient_MapMessage_MapsMetadataFields()
    {
        var settings = new AppSettings { GmailAccountLabel = "Personal" };
        var client = new GmailClient(settings);
        var method = typeof(GmailClient).GetMethod("MapMessage", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null);

        var message = new Message
        {
            Payload = new MessagePart
            {
                Headers =
                [
                    new MessagePartHeader { Name = "From", Value = "Display <sender@example.com>" },
                    new MessagePartHeader { Name = "Subject", Value = "Sample Subject" },
                    new MessagePartHeader { Name = "Date", Value = "Mon, 02 Mar 2026 10:00:00 +0000" }
                ]
            }
        };

        var mapped = (MailboxMessage)method!.Invoke(client, [message])!;

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Provider, Is.EqualTo("gmail"));
            Assert.That(mapped.AccountLabel, Is.EqualTo("Personal"));
            Assert.That(mapped.SenderAddress, Is.EqualTo("sender@example.com"));
            Assert.That(mapped.Subject, Is.EqualTo("Sample Subject"));
            Assert.That(mapped.ReceivedDateTime, Is.Not.Null);
        });
    }

    [Test]
    public void OutlookGraphClient_PrivateHelpers_ReturnExpectedValues()
    {
        var settings = new AppSettings
        {
            ClientId = "valid-client",
            TenantId = "consumers",
            PageSize = 42,
            AuthenticationRecordPath = ""
        };

        using var client = new OutlookGraphClient(settings);

        var buildInitialUrl = typeof(OutlookGraphClient).GetMethod("BuildInitialUrl", BindingFlags.NonPublic | BindingFlags.Instance);
        var resolveAuthPath = typeof(OutlookGraphClient).GetMethod("ResolveAuthenticationRecordPath", BindingFlags.NonPublic | BindingFlags.Static);
        var loadAuthRecord = typeof(OutlookGraphClient).GetMethod("LoadAuthenticationRecord", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Multiple(() =>
        {
            Assert.That(buildInitialUrl, Is.Not.Null);
            Assert.That(resolveAuthPath, Is.Not.Null);
            Assert.That(loadAuthRecord, Is.Not.Null);
        });

        var url = (string)buildInitialUrl!.Invoke(client, null)!;
        var resolved = (string)resolveAuthPath!.Invoke(null, [settings])!;
        var auth = loadAuthRecord!.Invoke(client, null);

        Assert.Multiple(() =>
        {
            Assert.That(url, Does.Contain("$top=42"));
            Assert.That(url, Does.Contain("graph.microsoft.com"));
            Assert.That(resolved, Does.Contain(Path.Combine("MailGrabber", "auth-record.bin")));
            Assert.That(auth is null || auth.GetType().Name == "AuthenticationRecord", Is.True);
        });
    }

    [Test]
    public void GmailClient_AuthorizeAsync_ThrowsForMissingSecretsFile()
    {
        var client = new GmailClient(new AppSettings { GmailClientSecretsPath = "missing-file.json" });
        var method = typeof(GmailClient).GetMethod("AuthorizeAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null);

        var invocation = () => (Task)method!.Invoke(client, [CancellationToken.None])!;

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await invocation());
        Assert.That(ex!.Message, Does.Contain("client secrets file not found"));
    }

    [Test]
    public void GmailClient_GetInboxMessagesAsync_ThrowsForMissingSecretsFile()
    {
        var client = new GmailClient(new AppSettings
        {
            GmailClientSecretsPath = "missing-file.json",
            MaxMessages = 1
        });

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetInboxMessagesAsync());
        Assert.That(ex!.Message, Does.Contain("client secrets file not found"));
    }
}
