using System.Text.Json;
using MailGrabber.Models;

namespace MailGrabber.Services;

public static class ConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static AppSettings Load(string[] args)
    {
        var configPath = ResolveConfigPath(args);
        var settings = File.Exists(configPath)
            ? LoadFromFile(configPath)
            : new AppSettings();

        ApplyEnvironmentOverrides(settings);
        ApplyArgumentOverrides(settings, args);

        return settings;
    }

    private static AppSettings LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }

    private static string ResolveConfigPath(string[] args)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index] == "--config")
            {
                return args[index + 1];
            }
        }

        var currentDirectoryCandidate = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        if (File.Exists(currentDirectoryCandidate))
        {
            return currentDirectoryCandidate;
        }

        return Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    private static void ApplyEnvironmentOverrides(AppSettings settings)
    {
        if (bool.TryParse(GetEnvironmentValue("MAILGRABBER_ENABLE_OUTLOOK"), out var enableOutlook))
        {
            settings.EnableOutlook = enableOutlook;
        }

        if (bool.TryParse(GetEnvironmentValue("MAILGRABBER_ENABLE_GMAIL"), out var enableGmail))
        {
            settings.EnableGmail = enableGmail;
        }

        settings.OutlookAccountLabel = GetEnvironmentValue("MAILGRABBER_OUTLOOK_ACCOUNT_LABEL") ?? settings.OutlookAccountLabel;
        settings.ClientId = GetEnvironmentValue("MAILGRABBER_CLIENT_ID") ?? settings.ClientId;
        settings.TenantId = GetEnvironmentValue("MAILGRABBER_TENANT_ID") ?? settings.TenantId;
        settings.GmailAccountLabel = GetEnvironmentValue("MAILGRABBER_GMAIL_ACCOUNT_LABEL") ?? settings.GmailAccountLabel;
        settings.GmailClientSecretsPath = GetEnvironmentValue("MAILGRABBER_GMAIL_CLIENT_SECRETS_PATH") ?? settings.GmailClientSecretsPath;
        settings.GmailTokenDirectory = GetEnvironmentValue("MAILGRABBER_GMAIL_TOKEN_DIRECTORY") ?? settings.GmailTokenDirectory;
        settings.GmailUserId = GetEnvironmentValue("MAILGRABBER_GMAIL_USER_ID") ?? settings.GmailUserId;
        settings.OutputPath = GetEnvironmentValue("MAILGRABBER_OUTPUT_PATH") ?? settings.OutputPath;
        settings.JsonOutputPath = GetEnvironmentValue("MAILGRABBER_JSON_OUTPUT_PATH") ?? settings.JsonOutputPath;
        settings.HtmlOutputPath = GetEnvironmentValue("MAILGRABBER_HTML_OUTPUT_PATH") ?? settings.HtmlOutputPath;

        if (bool.TryParse(GetEnvironmentValue("MAILGRABBER_WRITE_CSV"), out var writeCsv))
        {
            settings.WriteCsv = writeCsv;
        }

        if (bool.TryParse(GetEnvironmentValue("MAILGRABBER_WRITE_JSON"), out var writeJson))
        {
            settings.WriteJson = writeJson;
        }

        if (bool.TryParse(GetEnvironmentValue("MAILGRABBER_WRITE_HTML_VIEWER"), out var writeHtmlViewer))
        {
            settings.WriteHtmlViewer = writeHtmlViewer;
        }

        settings.AuthenticationRecordPath = GetEnvironmentValue("MAILGRABBER_AUTH_RECORD_PATH") ?? settings.AuthenticationRecordPath;
        settings.TokenCacheName = GetEnvironmentValue("MAILGRABBER_TOKEN_CACHE_NAME") ?? settings.TokenCacheName;

        if (bool.TryParse(GetEnvironmentValue("MAILGRABBER_ALLOW_UNENCRYPTED_TOKEN_CACHE"), out var allowUnencryptedTokenCache))
        {
            settings.AllowUnencryptedTokenCache = allowUnencryptedTokenCache;
        }

        if (int.TryParse(GetEnvironmentValue("MAILGRABBER_MAX_MESSAGES"), out var maxMessages))
        {
            settings.MaxMessages = maxMessages;
        }

        if (int.TryParse(GetEnvironmentValue("MAILGRABBER_PAGE_SIZE"), out var pageSize))
        {
            settings.PageSize = pageSize;
        }
    }

    private static void ApplyArgumentOverrides(AppSettings settings, string[] args)
    {
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (index == args.Length - 1)
            {
                continue;
            }

            switch (argument)
            {
                case "--client-id":
                    settings.ClientId = args[index + 1];
                    index++;
                    break;
                case "--enable-outlook" when bool.TryParse(args[index + 1], out var enableOutlook):
                    settings.EnableOutlook = enableOutlook;
                    index++;
                    break;
                case "--enable-gmail" when bool.TryParse(args[index + 1], out var enableGmail):
                    settings.EnableGmail = enableGmail;
                    index++;
                    break;
                case "--outlook-account-label":
                    settings.OutlookAccountLabel = args[index + 1];
                    index++;
                    break;
                case "--tenant-id":
                    settings.TenantId = args[index + 1];
                    index++;
                    break;
                case "--gmail-account-label":
                    settings.GmailAccountLabel = args[index + 1];
                    index++;
                    break;
                case "--gmail-client-secrets-path":
                    settings.GmailClientSecretsPath = args[index + 1];
                    index++;
                    break;
                case "--gmail-token-directory":
                    settings.GmailTokenDirectory = args[index + 1];
                    index++;
                    break;
                case "--gmail-user-id":
                    settings.GmailUserId = args[index + 1];
                    index++;
                    break;
                case "--output":
                    settings.OutputPath = args[index + 1];
                    index++;
                    break;
                case "--json-output":
                    settings.JsonOutputPath = args[index + 1];
                    index++;
                    break;
                case "--html-output":
                    settings.HtmlOutputPath = args[index + 1];
                    index++;
                    break;
                case "--write-csv" when bool.TryParse(args[index + 1], out var writeCsv):
                    settings.WriteCsv = writeCsv;
                    index++;
                    break;
                case "--write-json" when bool.TryParse(args[index + 1], out var writeJson):
                    settings.WriteJson = writeJson;
                    index++;
                    break;
                case "--write-html-viewer" when bool.TryParse(args[index + 1], out var writeHtmlViewer):
                    settings.WriteHtmlViewer = writeHtmlViewer;
                    index++;
                    break;
                case "--auth-record-path":
                    settings.AuthenticationRecordPath = args[index + 1];
                    index++;
                    break;
                case "--token-cache-name":
                    settings.TokenCacheName = args[index + 1];
                    index++;
                    break;
                case "--allow-unencrypted-token-cache" when bool.TryParse(args[index + 1], out var allowUnencryptedTokenCache):
                    settings.AllowUnencryptedTokenCache = allowUnencryptedTokenCache;
                    index++;
                    break;
                case "--max-messages" when int.TryParse(args[index + 1], out var maxMessages):
                    settings.MaxMessages = maxMessages;
                    index++;
                    break;
                case "--page-size" when int.TryParse(args[index + 1], out var pageSize):
                    settings.PageSize = pageSize;
                    index++;
                    break;
            }
        }
    }

    private static string? GetEnvironmentValue(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}