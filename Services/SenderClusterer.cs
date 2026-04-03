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
            var isNewsletter = settings.EnableNewsletterClustering && IsNewsletter(address, senderName, domain, settings);
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

        var rawDomain = address[(atIndex + 1)..].Trim('.');
        return NormalizeDomain(rawDomain);
    }

    private static string? NormalizeDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        var labels = domain.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        if (labels.Length < 2)
        {
            return domain;
        }

        if (labels.Length == 2)
        {
            return domain;
        }

        var tld2LevelHints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "co.uk", "co.nz", "co.za", "co.in", "co.il", "co.jp", "co.kr", "co.id", "co.th", "co.ug",
            "co.ve", "co.tz", "co.ke", "co.pk", "co.bd", "co.ng", "co.hn", "co.cr", "co.sv", "co.gt",
            "co.ni", "co.pa", "co.do", "co.cu", "co.mz", "co.rw", "co.sn", "co.gh", "co.zm",
            "com.br", "com.mx", "com.ar", "com.au", "com.ua", "com.tr", "com.kz", "com.vn", "com.hk",
            "com.sg", "com.my", "com.ph", "com.tw", "com.cn", "com.pk", "com.bd", "com.ng", "com.gh",
            "com.eg", "com.sa", "com.ae", "com.jo", "com.lb", "com.qa", "com.kw", "com.om", "com.bh",
            "com.bs", "com.bz", "com.fj", "com.pg", "com.sb", "com.ws", "com.vc", "com.lc", "com.gd",
            "com.bb", "org.uk", "org.nz", "org.za", "org.in", "org.il", "org.br", "org.mx", "org.au",
            "org.ru", "org.ua", "org.by", "org.kz", "org.tr", "org.hk", "org.sg", "org.my", "org.tw",
            "org.cn", "org.ph", "org.vn", "org.th", "org.id", "org.bd", "org.pk", "org.ng", "org.gh",
            "org.za", "ac.uk", "ac.nz", "ac.za", "ac.in", "ac.jp", "ac.kr", "ac.th", "ac.ug", "ac.id",
            "ac.bd", "ac.ke", "ac.tz", "ac.uy", "ac.ve", "ac.kr", "ac.ir", "gov.uk", "gov.au", "gov.br",
            "gov.in", "gov.hk", "gov.sg", "gov.my", "gov.ph", "gov.th", "gov.ua", "gov.ar", "gov.mx",
            "gov.za", "gov.ng", "net.uk", "net.nz", "net.au", "net.br", "net.mx", "net.in", "net.hk",
            "net.sg", "net.my", "net.tw", "net.vn", "net.th", "net.ua", "net.ru", "net.tr", "net.pk",
            "net.bd", "net.ng", "net.ao", "net.tz", "net.zw", "net.zm", "net.ke", "ac.ae", "co.ae",
            "sch.uk", "as.uk", "nhs.uk", "police.uk", "parliament.uk", "ltd.uk", "plc.uk"
        };

        var potential2LevelTld = $"{labels[^2]}.{labels[^1]}";
        var is2LevelTld = tld2LevelHints.Contains(potential2LevelTld);

        if (is2LevelTld && labels.Length >= 3)
        {
            return string.Join(".", labels.Skip(labels.Length - 3).Take(3));
        }

        if (labels.Length >= 2)
        {
            return string.Join(".", labels.Skip(labels.Length - 2).Take(2));
        }

        return domain;
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