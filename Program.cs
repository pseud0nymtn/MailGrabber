using Avalonia;
using MailGrabber.Views;
using MailGrabber.Services;
using System.Diagnostics.CodeAnalysis;

namespace MailGrabber;

[ExcludeFromCodeCoverage]
internal static class Program
{
	private static async Task<int> Main(string[] args)
	{
		if (args.Any(argument => argument is "--help" or "-h"))
		{
			ShowHelp();
			return 0;
		}

		var runCli = args.Any(argument => argument.Equals("--cli", StringComparison.OrdinalIgnoreCase));
		if (!runCli)
		{
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
			return 0;
		}

		var cliArguments = args.Where(argument => !argument.Equals("--cli", StringComparison.OrdinalIgnoreCase)).ToArray();

		try
		{
			var settings = ConfigurationLoader.Load(cliArguments);
			settings.Validate();

			Console.WriteLine($"Outlook enabled: {settings.EnableOutlook}");
			Console.WriteLine($"Gmail enabled: {settings.EnableGmail}");
			Console.WriteLine($"CSV output: {Path.GetFullPath(settings.OutputPath)}");
			Console.WriteLine($"JSON output: {Path.GetFullPath(settings.JsonOutputPath)}");
			Console.WriteLine($"HTML viewer: {Path.GetFullPath(settings.HtmlOutputPath)}");
			Console.WriteLine($"Max messages: {settings.MaxMessages}");
			Console.WriteLine();

			var result = await MailGrabberRunner.RunAsync(settings);

			Console.WriteLine($"Fetched {result.TotalMessages} inbox messages across all enabled providers.");
			Console.WriteLine($"Prepared {result.RowCount} clustered sender rows.");

			if (settings.WriteCsv)
			{
				Console.WriteLine($"CSV written to {Path.GetFullPath(settings.OutputPath)}");
			}

			if (settings.WriteJson)
			{
				Console.WriteLine($"JSON written to {Path.GetFullPath(settings.JsonOutputPath)}");
			}

			if (settings.WriteHtmlViewer)
			{
				Console.WriteLine($"Viewer written to {Path.GetFullPath(settings.HtmlOutputPath)}");
			}

			return 0;
		}
		catch (Exception exception)
		{
			Console.Error.WriteLine(exception.Message);
			return 1;
		}
	}

	public static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
	}

	private static void ShowHelp()
	{
		Console.WriteLine("MailGrabber - Desktop UI and CLI sender clustering");
		Console.WriteLine();
		Console.WriteLine("Usage:");
		Console.WriteLine("  dotnet run --                  (starts Avalonia desktop UI)");
		Console.WriteLine("  dotnet run -- --cli [--config appsettings.json] [--output output/sender-clusters.csv] [--json-output output/sender-clusters.json] [--html-output output/cluster-viewer.html]");
		Console.WriteLine();
		Console.WriteLine("Environment variable overrides:");
		Console.WriteLine("  MAILGRABBER_ENABLE_OUTLOOK");
		Console.WriteLine("  MAILGRABBER_ENABLE_GMAIL");
		Console.WriteLine("  MAILGRABBER_CLIENT_ID");
		Console.WriteLine("  MAILGRABBER_TENANT_ID");
		Console.WriteLine("  MAILGRABBER_GMAIL_CLIENT_SECRETS_PATH");
		Console.WriteLine("  MAILGRABBER_GMAIL_TOKEN_DIRECTORY");
		Console.WriteLine("  MAILGRABBER_OUTPUT_PATH");
		Console.WriteLine("  MAILGRABBER_JSON_OUTPUT_PATH");
		Console.WriteLine("  MAILGRABBER_HTML_OUTPUT_PATH");
		Console.WriteLine("  MAILGRABBER_WRITE_CSV");
		Console.WriteLine("  MAILGRABBER_WRITE_JSON");
		Console.WriteLine("  MAILGRABBER_WRITE_HTML_VIEWER");
		Console.WriteLine("  MAILGRABBER_AUTH_RECORD_PATH");
		Console.WriteLine("  MAILGRABBER_TOKEN_CACHE_NAME");
		Console.WriteLine("  MAILGRABBER_ALLOW_UNENCRYPTED_TOKEN_CACHE");
		Console.WriteLine("  MAILGRABBER_MAX_MESSAGES");
		Console.WriteLine("  MAILGRABBER_PAGE_SIZE");
	}
}
