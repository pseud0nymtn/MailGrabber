using System.Text;
using MailGrabber.Models;

namespace MailGrabber.Services;

public static class CsvExporter
{
    public static void Write(string outputPath, IReadOnlyCollection<ClusteredSenderRow> rows)
    {
        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(fullPath, false, new UTF8Encoding(false));
        writer.WriteLine("Cluster,Tld,Domain,SenderAddress,SenderName,Providers,SourceAccounts,MessageCount,FirstSeenUtc,LastSeenUtc,IsNewsletter,SampleSubjects");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",",
                Escape(row.Cluster),
                Escape(row.Tld),
                Escape(row.Domain),
                Escape(row.SenderAddress),
                Escape(row.SenderName),
                Escape(string.Join(" | ", row.Providers)),
                Escape(string.Join(" | ", row.SourceAccounts)),
                row.MessageCount,
                Escape(row.FirstSeenUtc?.ToUniversalTime().ToString("O") ?? string.Empty),
                Escape(row.LastSeenUtc?.ToUniversalTime().ToString("O") ?? string.Empty),
                row.IsNewsletter ? "true" : "false",
                Escape(string.Join(" | ", row.SampleSubjects))));
        }
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}