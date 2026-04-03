using Nager.PublicSuffix;
using Nager.PublicSuffix.Extensions;
using Nager.PublicSuffix.Models;
using Nager.PublicSuffix.RuleParsers;
using Nager.PublicSuffix.RuleProviders;

namespace MailGrabber.Services;

/// <summary>
/// Lazy-initialized wrapper around Nager.PublicSuffix's DomainParser, loaded from the
/// embedded Mozilla Public Suffix List (Resources/public_suffix_list.dat).
/// </summary>
internal static class PublicSuffixHelper
{
    private static readonly Lazy<DomainParser> LazyParser = new(BuildParser);

    private static DomainParser BuildParser()
    {
        var assembly = typeof(PublicSuffixHelper).Assembly;
        const string resourceName = "MailGrabber.Resources.public_suffix_list.dat";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        var ruleParser = new TldRuleParser(TldRuleDivisionFilter.All);
        var rules = ruleParser.ParseRules(content);

        var structure = new DomainDataStructure("*", new TldRule("*"));
        structure.AddRules(rules);

        var provider = new StaticRuleProvider(structure);
        return new DomainParser(provider);
    }

    /// <summary>
    /// Returns the registrable domain (eTLD+1) for a given hostname,
    /// or the raw hostname if it cannot be parsed.
    /// </summary>
    public static string? GetRegistrableDomain(string? hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            return null;
        }

        try
        {
            var info = LazyParser.Value.Parse(hostname);
            return string.IsNullOrWhiteSpace(info?.RegistrableDomain) ? hostname : info.RegistrableDomain;
        }
        catch
        {
            // Unknown or private TLD – fall back to the raw hostname.
            return hostname;
        }
    }

    /// <summary>
    /// Returns the effective TLD (e.g. "co.uk", "com") for a given hostname.
    /// </summary>
    public static string? GetTld(string? hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            return null;
        }

        try
        {
            var info = LazyParser.Value.Parse(hostname);
            return string.IsNullOrWhiteSpace(info?.TopLevelDomain) ? null : info.TopLevelDomain;
        }
        catch
        {
            // Fall back to the last label.
            var dot = hostname.LastIndexOf('.');
            return dot >= 0 ? hostname[(dot + 1)..] : hostname;
        }
    }
}
