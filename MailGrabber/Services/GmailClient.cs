using System.Globalization;
using System.Net.Mail;
using System.Diagnostics.CodeAnalysis;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MailGrabber.Models;

namespace MailGrabber.Services;

[ExcludeFromCodeCoverage]
public sealed class GmailClient
{
    private static readonly string[] GmailScopes = [GmailService.Scope.GmailReadonly];
    private readonly AppSettings _settings;

    public GmailClient(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<List<MailboxMessage>> GetInboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        var credential = await AuthorizeAsync(cancellationToken);
        using var service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "MailGrabber"
        });

        var results = new List<MailboxMessage>();
        string? nextPageToken = null;
        var query = BuildInboxQuery();

        while (results.Count < _settings.MaxMessages)
        {
            var listRequest = service.Users.Messages.List(_settings.GmailUserId);
            listRequest.LabelIds = new[] { "INBOX" };
            listRequest.Q = query;
            listRequest.MaxResults = Math.Min(500, _settings.MaxMessages - results.Count);
            listRequest.PageToken = nextPageToken;

            var page = await listRequest.ExecuteAsync(cancellationToken);
            if (page.Messages is null || page.Messages.Count == 0)
            {
                break;
            }

            foreach (var pageMessage in page.Messages)
            {
                var detailsRequest = service.Users.Messages.Get(_settings.GmailUserId, pageMessage.Id);
                detailsRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                detailsRequest.MetadataHeaders = new[] { "From", "Subject", "Date" };

                var message = await detailsRequest.ExecuteAsync(cancellationToken);
                results.Add(MapMessage(message));

                if (results.Count >= _settings.MaxMessages)
                {
                    break;
                }
            }

            Console.WriteLine($"Fetched {results.Count} gmail messages so far...");
            nextPageToken = page.NextPageToken;
            if (string.IsNullOrWhiteSpace(nextPageToken))
            {
                break;
            }
        }

        return results;
    }

    private string? BuildInboxQuery()
    {
        if (_settings.OldestMessageAgeDays <= 0)
        {
            return null;
        }

        var cutoffUnixSeconds = DateTimeOffset.UtcNow.AddDays(-_settings.OldestMessageAgeDays).ToUnixTimeSeconds();
        return $"after:{cutoffUnixSeconds}";
    }

    private async Task<UserCredential> AuthorizeAsync(CancellationToken cancellationToken)
    {
        var secretsPath = Path.GetFullPath(_settings.GmailClientSecretsPath);
        if (!File.Exists(secretsPath))
        {
            throw new InvalidOperationException($"Gmail client secrets file not found: {secretsPath}");
        }

        var tokenDirectory = ResolveTokenDirectory();
        Directory.CreateDirectory(tokenDirectory);

        await using var stream = File.OpenRead(secretsPath);
        var secrets = GoogleClientSecrets.FromStream(stream).Secrets;

        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            GmailScopes,
            "mailgrabber-user",
            cancellationToken,
            new FileDataStore(tokenDirectory, true));
    }

    private MailboxMessage MapMessage(Message message)
    {
        var headers = message.Payload?.Headers ?? [];
        var from = GetHeader(headers, "From");
        var subject = GetHeader(headers, "Subject");

        var (senderAddress, senderName) = ParseFromHeader(from);

        return new MailboxMessage
        {
            Provider = "gmail",
            AccountLabel = _settings.GmailAccountLabel,
            SenderAddress = senderAddress,
            SenderName = senderName,
            Subject = subject,
            ReceivedDateTime = ParseReceivedDate(message, headers)
        };
    }

    private static string? GetHeader(IEnumerable<MessagePartHeader> headers, string name)
    {
        return headers.FirstOrDefault(header => name.Equals(header.Name, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    private static (string? SenderAddress, string? SenderName) ParseFromHeader(string? from)
    {
        if (string.IsNullOrWhiteSpace(from))
        {
            return (null, null);
        }

        try
        {
            var address = new MailAddress(from);
            return (address.Address, address.DisplayName);
        }
        catch
        {
            var trimmed = from.Trim();
            if (trimmed.Contains('@'))
            {
                return (trimmed, string.Empty);
            }

            return (null, trimmed);
        }
    }

    private static DateTimeOffset? ParseReceivedDate(Message message, IEnumerable<MessagePartHeader> headers)
    {
        if (message.InternalDate.HasValue)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate.Value);
        }

        var dateHeader = GetHeader(headers, "Date");
        if (DateTimeOffset.TryParse(dateHeader, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private string ResolveTokenDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_settings.GmailTokenDirectory))
        {
            return Path.GetFullPath(_settings.GmailTokenDirectory);
        }

        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Directory.GetCurrentDirectory();
        }

        return Path.Combine(baseDirectory, "MailGrabber", "gmail-token");
    }
}
