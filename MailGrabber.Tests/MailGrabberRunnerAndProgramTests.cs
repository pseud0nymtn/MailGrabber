using System.Reflection;
using MailGrabber.Models;
using MailGrabber.Services;

namespace MailGrabber.Tests;

public class MailGrabberRunnerAndProgramTests
{
    [Test]
    public async Task MailGrabberRunner_RunAsync_WritesSelectedOutputs()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var settings = new AppSettings
            {
                EnableOutlook = false,
                EnableGmail = false,
                ClientId = "client",
                WriteCsv = true,
                WriteJson = true,
                WriteHtmlViewer = true,
                OutputPath = Path.Combine(tempDir, "rows.csv"),
                JsonOutputPath = Path.Combine(tempDir, "report.json"),
                HtmlOutputPath = Path.Combine(tempDir, "viewer.html")
            };

            var logs = new List<string>();
            var result = await MailGrabberRunner.RunAsync(settings, logs.Add);

            Assert.Multiple(() =>
            {
                Assert.That(result.TotalMessages, Is.EqualTo(0));
                Assert.That(result.RowCount, Is.EqualTo(0));
                Assert.That(File.Exists(settings.OutputPath), Is.True);
                Assert.That(File.Exists(settings.JsonOutputPath), Is.True);
                Assert.That(File.Exists(settings.HtmlOutputPath), Is.True);
                Assert.That(logs.Any(l => l.Contains("CSV written")), Is.True);
                Assert.That(logs.Any(l => l.Contains("JSON written")), Is.True);
                Assert.That(logs.Any(l => l.Contains("Viewer written")), Is.True);
            });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task Program_Main_WithHelpArgument_ReturnsZeroAndPrintsUsage()
    {
        var mainMethod = typeof(Program).GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(mainMethod, Is.Not.Null);

        var originalOut = Console.Out;
        await using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            var task = (Task<int>)mainMethod!.Invoke(null, [new[] { "--help" }])!;
            var result = await task;

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(0));
                Assert.That(writer.ToString(), Does.Contain("Usage:"));
            });
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public void Program_BuildAvaloniaApp_ReturnsBuilder()
    {
        var builder = Program.BuildAvaloniaApp();

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public async Task Program_Main_WithCliInvalidSettings_ReturnsOne()
    {
        var mainMethod = typeof(Program).GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(mainMethod, Is.Not.Null);

        var originalError = Console.Error;
        using var writer = new StringWriter();
        Console.SetError(writer);

        try
        {
            var task = (Task<int>)mainMethod!.Invoke(null, [new[] { "--cli", "--client-id", "YOUR-CLIENT-ID-HERE" }])!;
            var result = await task;

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(1));
                Assert.That(writer.ToString(), Is.Not.Empty);
            });
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Test]
    public void MailGrabberRunResult_RecordStoresValues()
    {
        var report = new ClusterReport();
        var result = new MailGrabberRunResult(7, 3, report);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalMessages, Is.EqualTo(7));
            Assert.That(result.RowCount, Is.EqualTo(3));
            Assert.That(result.Report, Is.SameAs(report));
        });
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "mailgrabber-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
