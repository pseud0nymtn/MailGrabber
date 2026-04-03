using System.Text.Json;
using MailGrabber.Models;

namespace MailGrabber.Services;

public static class JsonExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Write(string outputPath, ClusterReport report)
    {
        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(fullPath, json);
    }
}
