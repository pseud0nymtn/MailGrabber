using System.Text.Json;
using MailGrabber.Models;
using MailGrabber.Services;
using MailGrabber.Tests.TestHelpers;

namespace MailGrabber.Tests;

public class ConfigurationLoaderTests
{
    [Test]
    public void Load_ReadsFromExplicitConfig_AndAppliesArgumentOverrides()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var configPath = Path.Combine(tempDir, "settings.json");
            var settings = new AppSettings
            {
                EnableOutlook = true,
                EnableGmail = false,
                ClientId = "from-file",
                OutputPath = "file.csv",
                JsonOutputPath = "file.json",
                HtmlOutputPath = "file.html",
                MaxMessages = 10,
                PageSize = 20
            };
            File.WriteAllText(configPath, JsonSerializer.Serialize(settings));

            var loaded = ConfigurationLoader.Load([
                "--config", configPath,
                "--client-id", "from-arg",
                "--enable-gmail", "true",
                "--output", "arg.csv",
                "--max-messages", "99",
                "--page-size", "33"
            ]);

            Assert.Multiple(() =>
            {
                Assert.That(loaded.ClientId, Is.EqualTo("from-arg"));
                Assert.That(loaded.EnableGmail, Is.True);
                Assert.That(loaded.OutputPath, Is.EqualTo("arg.csv"));
                Assert.That(loaded.MaxMessages, Is.EqualTo(99));
                Assert.That(loaded.PageSize, Is.EqualTo(33));
            });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void Load_AppliesEnvironmentOverrides()
    {
        using var env = new EnvVarScope(new Dictionary<string, string?>
        {
            ["MAILGRABBER_ENABLE_OUTLOOK"] = "false",
            ["MAILGRABBER_ENABLE_GMAIL"] = "true",
            ["MAILGRABBER_CLIENT_ID"] = "env-client",
            ["MAILGRABBER_TENANT_ID"] = "env-tenant",
            ["MAILGRABBER_GMAIL_ACCOUNT_LABEL"] = "Gmail Label",
            ["MAILGRABBER_OUTPUT_PATH"] = "out.csv",
            ["MAILGRABBER_JSON_OUTPUT_PATH"] = "out.json",
            ["MAILGRABBER_HTML_OUTPUT_PATH"] = "out.html",
            ["MAILGRABBER_WRITE_CSV"] = "false",
            ["MAILGRABBER_WRITE_JSON"] = "false",
            ["MAILGRABBER_WRITE_HTML_VIEWER"] = "true",
            ["MAILGRABBER_AUTH_RECORD_PATH"] = "auth.bin",
            ["MAILGRABBER_TOKEN_CACHE_NAME"] = "cache",
            ["MAILGRABBER_ALLOW_UNENCRYPTED_TOKEN_CACHE"] = "false",
            ["MAILGRABBER_MAX_MESSAGES"] = "123",
            ["MAILGRABBER_PAGE_SIZE"] = "77"
        });

        var loaded = ConfigurationLoader.Load(Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.That(loaded.EnableOutlook, Is.False);
            Assert.That(loaded.EnableGmail, Is.True);
            Assert.That(loaded.ClientId, Is.EqualTo("env-client"));
            Assert.That(loaded.TenantId, Is.EqualTo("env-tenant"));
            Assert.That(loaded.GmailAccountLabel, Is.EqualTo("Gmail Label"));
            Assert.That(loaded.OutputPath, Is.EqualTo("out.csv"));
            Assert.That(loaded.JsonOutputPath, Is.EqualTo("out.json"));
            Assert.That(loaded.HtmlOutputPath, Is.EqualTo("out.html"));
            Assert.That(loaded.WriteCsv, Is.False);
            Assert.That(loaded.WriteJson, Is.False);
            Assert.That(loaded.WriteHtmlViewer, Is.True);
            Assert.That(loaded.AuthenticationRecordPath, Is.EqualTo("auth.bin"));
            Assert.That(loaded.TokenCacheName, Is.EqualTo("cache"));
            Assert.That(loaded.AllowUnencryptedTokenCache, Is.False);
            Assert.That(loaded.MaxMessages, Is.EqualTo(123));
            Assert.That(loaded.PageSize, Is.EqualTo(77));
        });
    }

    [Test]
    public void Load_UsesDefaults_WhenConfigMissing()
    {
        var loaded = ConfigurationLoader.Load(["--config", Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")]);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded.OutputPath, Is.Not.Empty);
    }

    [Test]
    public void Load_ParsesManyArgumentOverrides()
    {
        var loaded = ConfigurationLoader.Load([
            "--enable-outlook", "false",
            "--enable-gmail", "true",
            "--outlook-account-label", "Out",
            "--tenant-id", "ten",
            "--gmail-account-label", "Gm",
            "--gmail-client-secrets-path", "secret.json",
            "--gmail-token-directory", "tokens",
            "--gmail-user-id", "me2",
            "--json-output", "report.json",
            "--html-output", "viewer.html",
            "--write-csv", "false",
            "--write-json", "false",
            "--write-html-viewer", "true",
            "--auth-record-path", "auth.bin",
            "--token-cache-name", "cache-1",
            "--allow-unencrypted-token-cache", "false"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.EnableOutlook, Is.False);
            Assert.That(loaded.EnableGmail, Is.True);
            Assert.That(loaded.OutlookAccountLabel, Is.EqualTo("Out"));
            Assert.That(loaded.TenantId, Is.EqualTo("ten"));
            Assert.That(loaded.GmailAccountLabel, Is.EqualTo("Gm"));
            Assert.That(loaded.GmailClientSecretsPath, Is.EqualTo("secret.json"));
            Assert.That(loaded.GmailTokenDirectory, Is.EqualTo("tokens"));
            Assert.That(loaded.GmailUserId, Is.EqualTo("me2"));
            Assert.That(loaded.JsonOutputPath, Is.EqualTo("report.json"));
            Assert.That(loaded.HtmlOutputPath, Is.EqualTo("viewer.html"));
            Assert.That(loaded.WriteCsv, Is.False);
            Assert.That(loaded.WriteJson, Is.False);
            Assert.That(loaded.WriteHtmlViewer, Is.True);
            Assert.That(loaded.AuthenticationRecordPath, Is.EqualTo("auth.bin"));
            Assert.That(loaded.TokenCacheName, Is.EqualTo("cache-1"));
            Assert.That(loaded.AllowUnencryptedTokenCache, Is.False);
        });
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "mailgrabber-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
