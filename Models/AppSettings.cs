namespace MailGrabber.Models;

public sealed class AppSettings
{
    public bool EnableOutlook { get; set; } = true;

    public string OutlookAccountLabel { get; set; } = "Outlook";

    public string ClientId { get; set; } = "YOUR-CLIENT-ID-HERE";

    public string TenantId { get; set; } = "consumers";

    public bool EnableGmail { get; set; } = false;

    public string GmailAccountLabel { get; set; } = "Gmail";

    public string GmailClientSecretsPath { get; set; } = "google-client-secret.json";

    public string GmailTokenDirectory { get; set; } = string.Empty;

    public string GmailUserId { get; set; } = "me";

    public string OutputPath { get; set; } = "output/sender-clusters.csv";

    public string JsonOutputPath { get; set; } = "output/sender-clusters.json";

    public string HtmlOutputPath { get; set; } = "output/cluster-viewer.html";

    public bool WriteCsv { get; set; } = true;

    public bool WriteJson { get; set; } = true;

    public bool WriteHtmlViewer { get; set; } = true;

    public string AuthenticationRecordPath { get; set; } = string.Empty;

    public string TokenCacheName { get; set; } = "MailGrabberTokenCache";

    public bool AllowUnencryptedTokenCache { get; set; } = true;

    public int MaxMessages { get; set; } = 2000;

    public int PageSize { get; set; } = 50;

    public bool EnableNewsletterClustering { get; set; } = true;

    public string NewsletterClusterName { get; set; } = "newsletter";

    public List<string> NewsletterMailboxHints { get; set; } =
    [
        "newsletter",
        "news",
        "noreply",
        "no-reply",
        "donotreply",
        "do-not-reply",
        "mailer",
        "mailing",
        "digest",
        "updates",
        "update",
        "notification",
        "notifications",
        "hello",
        "info",
        "offers",
        "marketing",
        "promo",
        "community",
        "kontakt",
        "service"
    ];

    public List<string> NewsletterDomainHints { get; set; } =
    [
        "mailchimp",
        "sendgrid",
        "mailjet",
        "mailer",
        "mailing",
        "newsletter",
        "emarketing",
        "campaign",
        "postmaster",
        "sparkpost",
        "constantcontact",
        "hubspot"
    ];

    public string[] GraphScopes => ["Mail.Read", "User.Read"];

    public void Validate()
    {
        if (!EnableOutlook && !EnableGmail)
        {
            throw new InvalidOperationException("At least one provider must be enabled: EnableOutlook or EnableGmail.");
        }

        if (EnableOutlook && (string.IsNullOrWhiteSpace(ClientId) || ClientId.Equals("YOUR-CLIENT-ID-HERE", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("No Microsoft app client ID configured. Edit appsettings.json or set MAILGRABBER_CLIENT_ID.");
        }

        if (EnableGmail && string.IsNullOrWhiteSpace(GmailClientSecretsPath))
        {
            throw new InvalidOperationException("EnableGmail is true but GmailClientSecretsPath is empty.");
        }

        if (MaxMessages <= 0)
        {
            throw new InvalidOperationException("MaxMessages must be greater than 0.");
        }

        if (PageSize <= 0 || PageSize > 1000)
        {
            throw new InvalidOperationException("PageSize must be between 1 and 1000.");
        }
    }
}