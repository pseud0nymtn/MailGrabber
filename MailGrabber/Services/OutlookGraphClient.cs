using System.Net.Http.Headers;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;
using MailGrabber.Models;

namespace MailGrabber.Services;

[ExcludeFromCodeCoverage]
public sealed class OutlookGraphClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = new();
    private DeviceCodeCredential _credential;
    private readonly AppSettings _settings;
    private readonly string _authenticationRecordPath;
    private bool _hasAuthenticationRecord;

    public OutlookGraphClient(AppSettings settings)
    {
        _settings = settings;
        _authenticationRecordPath = ResolveAuthenticationRecordPath(settings);
        var authenticationRecord = LoadAuthenticationRecord();
        _hasAuthenticationRecord = authenticationRecord is not null;
        _credential = CreateCredential(authenticationRecord);
    }

    public async Task<List<MailboxMessage>> GetInboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

        var results = new List<MailboxMessage>();
        var nextPageUrl = BuildInitialUrl();

        while (!string.IsNullOrWhiteSpace(nextPageUrl) && results.Count < _settings.MaxMessages)
        {
            using var response = await _httpClient.GetAsync(nextPageUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Microsoft Graph request failed with {(int)response.StatusCode} {response.ReasonPhrase}: {responseBody}");
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var page = await JsonSerializer.DeserializeAsync<GraphMessagePage>(contentStream, JsonOptions, cancellationToken)
                ?? new GraphMessagePage();

            foreach (var message in page.Value)
            {
                var sender = message.Sender?.EmailAddress ?? message.From?.EmailAddress;
                results.Add(new MailboxMessage
                {
                    Provider = "outlook",
                    AccountLabel = _settings.OutlookAccountLabel,
                    SenderAddress = sender?.Address,
                    SenderName = sender?.Name,
                    Subject = message.Subject,
                    ReceivedDateTime = message.ReceivedDateTime
                });

                if (results.Count >= _settings.MaxMessages)
                {
                    break;
                }
            }

            Console.WriteLine($"Fetched {results.Count} messages so far...");
            nextPageUrl = results.Count >= _settings.MaxMessages ? null : page.NextLink;
        }

        return results;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private string BuildInitialUrl()
    {
        var select = Uri.EscapeDataString("subject,receivedDateTime,from,sender");
        var orderBy = Uri.EscapeDataString("receivedDateTime desc");
        var filterClause = BuildReceivedDateFilter();
        return $"https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages?$select={select}&$orderby={orderBy}&$top={_settings.PageSize}{filterClause}";
    }

    private string BuildReceivedDateFilter()
    {
        if (_settings.OldestMessageAgeDays <= 0)
        {
            return string.Empty;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-_settings.OldestMessageAgeDays);
        var isoCutoff = cutoff.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var filter = Uri.EscapeDataString($"receivedDateTime ge {isoCutoff}");
        return $"&$filter={filter}";
    }

    private DeviceCodeCredential CreateCredential(AuthenticationRecord? authenticationRecord)
    {
        return new DeviceCodeCredential(new DeviceCodeCredentialOptions
        {
            ClientId = _settings.ClientId,
            TenantId = _settings.TenantId,
            AuthenticationRecord = authenticationRecord,
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
            {
                Name = _settings.TokenCacheName,
                UnsafeAllowUnencryptedStorage = _settings.AllowUnencryptedTokenCache
            },
            DeviceCodeCallback = (deviceCodeInfo, _) =>
            {
                Console.WriteLine(deviceCodeInfo.Message);
                Console.WriteLine();
                return Task.CompletedTask;
            }
        });
    }

    private async Task<AccessToken> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var requestContext = new TokenRequestContext(_settings.GraphScopes);

        if (!_hasAuthenticationRecord)
        {
            var authenticationRecord = await _credential.AuthenticateAsync(requestContext, cancellationToken);
            await SaveAuthenticationRecordAsync(authenticationRecord, cancellationToken);
            _credential = CreateCredential(authenticationRecord);
            _hasAuthenticationRecord = true;
        }

        try
        {
            return await _credential.GetTokenAsync(requestContext, cancellationToken);
        }
        catch (AuthenticationRequiredException)
        {
            var authenticationRecord = await _credential.AuthenticateAsync(requestContext, cancellationToken);
            await SaveAuthenticationRecordAsync(authenticationRecord, cancellationToken);
            _credential = CreateCredential(authenticationRecord);
            _hasAuthenticationRecord = true;
            return await _credential.GetTokenAsync(requestContext, cancellationToken);
        }
    }

    private AuthenticationRecord? LoadAuthenticationRecord()
    {
        if (!File.Exists(_authenticationRecordPath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(_authenticationRecordPath);
            return AuthenticationRecord.Deserialize(stream, CancellationToken.None);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveAuthenticationRecordAsync(AuthenticationRecord authenticationRecord, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_authenticationRecordPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_authenticationRecordPath);
        await authenticationRecord.SerializeAsync(stream, cancellationToken);
    }

    private static string ResolveAuthenticationRecordPath(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.AuthenticationRecordPath))
        {
            return Path.GetFullPath(settings.AuthenticationRecordPath);
        }

        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Directory.GetCurrentDirectory();
        }

        return Path.Combine(baseDirectory, "MailGrabber", "auth-record.bin");
    }
}