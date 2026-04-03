using MailGrabber.Models;

namespace MailGrabber.Services;

public static class SenderClusterer
{
    public static List<ClusteredSenderRow> BuildRows(IEnumerable<MailboxMessage> messages, AppSettings settings)
    {
        var senders = new Dictionary<string, SenderAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var message in messages)
        {
            var address = NormalizeAddress(message.SenderAddress);
            if (address is null)
            {
                continue;
            }

            var senderName = message.SenderName?.Trim() ?? string.Empty;
            var domain = ExtractDomain(address);
            var tld = ExtractTld(domain);
            var isNewsletter = IsNewsletter(address, senderName, domain, settings);
            var cluster = isNewsletter ? settings.NewsletterClusterName : domain ?? "unknown";

            if (!senders.TryGetValue(address, out var accumulator))
            {
                accumulator = new SenderAccumulator(cluster, tld ?? "unknown", domain ?? "unknown", address, senderName, isNewsletter);
                senders.Add(address, accumulator);
            }

            accumulator.Add(message);
        }

        return senders
            .Values
            .Select(accumulator => accumulator.ToRow())
            .OrderBy(row => row.IsNewsletter ? 0 : 1)
            .ThenBy(row => row.Cluster, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(row => row.MessageCount)
            .ThenBy(row => row.SenderAddress, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? NormalizeAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var normalized = address.Trim().ToLowerInvariant();
        return normalized.Contains('@') ? normalized : null;
    }

    private static string? ExtractDomain(string address)
    {
        var atIndex = address.LastIndexOf('@');
        if (atIndex < 0 || atIndex == address.Length - 1)
        {
            return null;
        }

        return address[(atIndex + 1)..].Trim('.');
    }

    private static string? ExtractTld(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        var labels = domain.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return labels.Length == 0 ? null : labels[^1].ToLowerInvariant();
    }

    private static bool IsNewsletter(string address, string senderName, string? domain, AppSettings settings)
    {
        var mailbox = address.Split('@')[0];

        return ContainsHint(mailbox, settings.NewsletterMailboxHints)
            || ContainsHint(senderName, settings.NewsletterMailboxHints)
            || ContainsHint(domain, settings.NewsletterDomainHints);
    }

    private static bool ContainsHint(string? value, IEnumerable<string> hints)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return hints.Any(hint => value.Contains(hint, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class SenderAccumulator(
        string cluster,
        string tld,
        string domain,
        string address,
        string senderName,
        bool isNewsletter)
    {
        private readonly HashSet<string> _sampleSubjects = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _providers = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _sourceAccounts = new(StringComparer.OrdinalIgnoreCase);

        private DateTimeOffset? _firstSeenUtc;
        private DateTimeOffset? _lastSeenUtc;

        public int MessageCount { get; private set; }

        public void Add(MailboxMessage message)
        {
            MessageCount++;

            if (!string.IsNullOrWhiteSpace(message.Provider))
            {
                _providers.Add(message.Provider.Trim());
            }

            if (!string.IsNullOrWhiteSpace(message.AccountLabel))
            {
                _sourceAccounts.Add(message.AccountLabel.Trim());
            }

            if (!string.IsNullOrWhiteSpace(message.Subject) && _sampleSubjects.Count < 3)
            {
                _sampleSubjects.Add(message.Subject.Trim());
            }

            if (message.ReceivedDateTime is null)
            {
                return;
            }

            if (_firstSeenUtc is null || message.ReceivedDateTime < _firstSeenUtc)
            {
                _firstSeenUtc = message.ReceivedDateTime;
            }

            if (_lastSeenUtc is null || message.ReceivedDateTime > _lastSeenUtc)
            {
                _lastSeenUtc = message.ReceivedDateTime;
            }
        }

        public ClusteredSenderRow ToRow()
        {
            return new ClusteredSenderRow
            {
                Cluster = cluster,
                Tld = tld,
                Domain = domain,
                SenderAddress = address,
                SenderName = senderName,
                Providers = _providers.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList(),
                SourceAccounts = _sourceAccounts.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList(),
                MessageCount = MessageCount,
                FirstSeenUtc = _firstSeenUtc,
                LastSeenUtc = _lastSeenUtc,
                IsNewsletter = isNewsletter,
                SampleSubjects = _sampleSubjects.ToList()
            };
        }
    }
}