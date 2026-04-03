using MailGrabber.Models;
using System.Diagnostics.CodeAnalysis;

namespace MailGrabber.Services;

[ExcludeFromCodeCoverage]
public static class MailGrabberRunner
{
    public static async Task<MailGrabberRunResult> RunAsync(AppSettings settings, Action<string>? log = null)
    {
        var messages = new List<MailboxMessage>();

        if (settings.EnableOutlook)
        {
            log?.Invoke("Reading Outlook inbox...");
            using var outlookClient = new OutlookGraphClient(settings);
            messages.AddRange(await outlookClient.GetInboxMessagesAsync());
            log?.Invoke("Outlook inbox finished.");
        }

        if (settings.EnableGmail)
        {
            log?.Invoke("Reading Gmail inbox...");
            var gmailClient = new GmailClient(settings);
            messages.AddRange(await gmailClient.GetInboxMessagesAsync());
            log?.Invoke("Gmail inbox finished.");
        }

        log?.Invoke("Clustering senders...");
        var rows = SenderClusterer.BuildRows(messages, settings);
        var report = ReportBuilder.Build(rows, messages.Count);

        if (settings.WriteCsv)
        {
            CsvExporter.Write(settings.OutputPath, rows);
            log?.Invoke($"CSV written: {Path.GetFullPath(settings.OutputPath)}");
        }

        if (settings.WriteJson)
        {
            JsonExporter.Write(settings.JsonOutputPath, report);
            log?.Invoke($"JSON written: {Path.GetFullPath(settings.JsonOutputPath)}");
        }

        if (settings.WriteHtmlViewer)
        {
            HtmlViewerExporter.Write(settings.HtmlOutputPath, report);
            log?.Invoke($"Viewer written: {Path.GetFullPath(settings.HtmlOutputPath)}");
        }

        return new MailGrabberRunResult(messages.Count, rows.Count, report);
    }
}

public sealed record MailGrabberRunResult(int TotalMessages, int RowCount, ClusterReport Report);
