using CommunityToolkit.Mvvm.ComponentModel;

namespace MailGrabber.Models;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty]
    private bool enableOutlook = true;

    [ObservableProperty]
    private string outlookAccountLabel = "Outlook";

    [ObservableProperty]
    private string clientId = "YOUR-CLIENT-ID-HERE";

    [ObservableProperty]
    private string tenantId = "consumers";

    [ObservableProperty]
    private bool enableGmail;

    [ObservableProperty]
    private string gmailAccountLabel = "Gmail";

    [ObservableProperty]
    private string gmailClientSecretsPath = "google-client-secret.json";

    [ObservableProperty]
    private string gmailTokenDirectory = string.Empty;

    [ObservableProperty]
    private string gmailUserId = "me";

    [ObservableProperty]
    private string outputPath = "output/sender-clusters.csv";

    [ObservableProperty]
    private string jsonOutputPath = "output/sender-clusters.json";

    [ObservableProperty]
    private string htmlOutputPath = "output/cluster-viewer.html";

    [ObservableProperty]
    private bool writeCsv = true;

    [ObservableProperty]
    private bool writeJson = true;

    [ObservableProperty]
    private bool writeHtmlViewer = true;

    [ObservableProperty]
    private string authenticationRecordPath = string.Empty;

    [ObservableProperty]
    private string tokenCacheName = "MailGrabberTokenCache";

    [ObservableProperty]
    private bool allowUnencryptedTokenCache = true;

    [ObservableProperty]
    private int maxMessages = 2000;

    [ObservableProperty]
    private int pageSize = 50;

    [ObservableProperty]
    private int oldestMessageAgeDays;

    [ObservableProperty]
    private bool enableNewsletterClustering = true;

    [ObservableProperty]
    private string newsletterClusterName = "newsletter";

    [ObservableProperty]
    private List<string> newsletterMailboxHints =
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

    [ObservableProperty]
    private List<string> newsletterDomainHints =
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

        if (OldestMessageAgeDays < 0)
        {
            throw new InvalidOperationException("OldestMessageAgeDays must be greater than or equal to 0.");
        }
    }
}
