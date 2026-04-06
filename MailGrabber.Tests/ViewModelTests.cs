using MailGrabber.Models;
using MailGrabber.ViewModels;
using System.Reflection;

namespace MailGrabber.Tests;

public class ViewModelTests
{
    [Test]
    public void ClusterBucketViewModel_MapsValuesAndBuildsSummary()
    {
        var bucket = new ClusterBucket
        {
            Cluster = "example.com",
            IsNewsletterCluster = true,
            SenderCount = 2,
            MessageCount = 5,
            SenderAddresses =
            [
                new ClusteredSenderRow
                {
                    SenderAddress = "a@example.com",
                    SenderName = "A",
                    Domain = "example.com",
                    MessageCount = 5,
                    Providers = ["gmail"],
                    SourceAccounts = ["acc"],
                    SampleSubjects = ["S"]
                }
            ]
        };

        var vm = new ClusterBucketViewModel(bucket);

        Assert.Multiple(() =>
        {
            Assert.That(vm.Cluster, Is.EqualTo("example.com"));
            Assert.That(vm.Summary, Is.EqualTo("2 senders / 5 messages"));
            Assert.That(vm.SenderAddresses.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void SelectableClusterViewModel_InvokesCallback_WhenSelectionChanges()
    {
        var callbackCount = 0;
        var bucket = new ClusterBucket { Cluster = "x", SenderCount = 1, MessageCount = 2 };
        var vm = new SelectableClusterViewModel(bucket, () => callbackCount++);

        vm.IsSelected = true;

        Assert.Multiple(() =>
        {
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(vm.DisplayText, Is.EqualTo("x (1 senders, 2 messages)"));
        });
    }

    [Test]
    public void SenderRowViewModel_MapsAndFormatsCollections()
    {
        var row = new ClusteredSenderRow
        {
            SenderAddress = "sender@example.com",
            SenderName = "Sender",
            Domain = "example.com",
            MessageCount = 3,
            Providers = ["gmail", "outlook"],
            SourceAccounts = ["private"],
            SampleSubjects = ["A", "B"]
        };

        var vm = new SenderRowViewModel(row);

        Assert.Multiple(() =>
        {
            Assert.That(vm.Providers, Is.EqualTo("gmail, outlook"));
            Assert.That(vm.Accounts, Is.EqualTo("private"));
            Assert.That(vm.SampleSubjects, Is.EqualTo("A | B"));
            Assert.That(vm.LastSeenDisplay, Is.EqualTo("-"));
        });
    }

    [Test]
    public void SettingsDialogViewModel_Commands_InvokeHandlers()
    {
        var vm = new SettingsDialogViewModel();
        var okCalled = false;
        var cancelCalled = false;
        vm.OnOk = () => okCalled = true;
        vm.OnCancel = () => cancelCalled = true;

        vm.OkCommand.Execute(null);
        vm.CancelCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(okCalled, Is.True);
            Assert.That(cancelCalled, Is.True);
        });
    }

    [Test]
    public void SettingsDialogViewModel_SaveToPath_PersistsOverriddenValues()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var path = Path.Combine(tempDir, "config", "appsettings.json");
            var vm = new SettingsDialogViewModel
            {
                BaseSettings = new AppSettings
                {
                    ClientId = "base-client",
                    TenantId = "base-tenant"
                },
                EnableOutlook = false,
                EnableGmail = true,
                ClientId = "view-client-id",
                GmailClientSecretsPath = "gmail-secret.json",
                EnableNewsletterClustering = false,
                MaxMessages = 77,
                OldestMessageAgeDays = 21,
                OutputPath = "x.csv",
                JsonOutputPath = "x.json",
                HtmlOutputPath = "x.html"
            };

            vm.SaveToPath(path);
            var written = File.ReadAllText(path);

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(path), Is.True);
                Assert.That(written, Does.Contain("\"EnableOutlook\": false"));
                Assert.That(written, Does.Contain("\"EnableGmail\": true"));
                Assert.That(written, Does.Contain("\"ClientId\": \"view-client-id\""));
                Assert.That(written, Does.Contain("\"GmailClientSecretsPath\": \"gmail-secret.json\""));
                Assert.That(written, Does.Contain("\"OldestMessageAgeDays\": 21"));
            });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task MainWindowViewModel_RunCommand_ExecutesAndUpdatesStatus()
    {
        var vm = new MainWindowViewModel();

        vm.ApplySettings(false, false, 10, 0, true, "appsettings.json", "", "google-client-secret.json", "a.csv", "a.json", "a.html");

        Assert.That(vm.RunCommand.CanExecute(null), Is.True);
        await vm.RunCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.IsBusy, Is.False);
            Assert.That(vm.StatusMessage, Does.StartWith("Done."));
            Assert.That(vm.RunLog, Does.Contain("Clustering senders..."));
        });
    }

    [Test]
    public void MainWindowViewModel_LoadReportRelatedFlow_UpdatesCollections()
    {
        var vm = new MainWindowViewModel();

        var report = new ClusterReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            TotalInputMessages = 2,
            Clusters =
            [
                new ClusterBucket
                {
                    Cluster = "news",
                    IsNewsletterCluster = true,
                    SenderCount = 1,
                    MessageCount = 1,
                    SenderAddresses =
                    [
                        new ClusteredSenderRow
                        {
                            Cluster = "news",
                            SenderAddress = "n@example.com",
                            SenderName = "News",
                            Domain = "example.com",
                            MessageCount = 1,
                            LastSeenUtc = DateTimeOffset.UtcNow,
                            Providers = ["gmail"],
                            SourceAccounts = ["acc"],
                            SampleSubjects = ["Hi"]
                        }
                    ]
                },
                new ClusterBucket
                {
                    Cluster = "work",
                    IsNewsletterCluster = false,
                    SenderCount = 1,
                    MessageCount = 1,
                    SenderAddresses =
                    [
                        new ClusteredSenderRow
                        {
                            Cluster = "work",
                            SenderAddress = "w@example.com",
                            SenderName = "Work",
                            Domain = "example.com",
                            MessageCount = 1,
                            LastSeenUtc = DateTimeOffset.UtcNow.AddDays(-30),
                            Providers = ["outlook"],
                            SourceAccounts = ["acc"],
                            SampleSubjects = ["Hi"]
                        }
                    ]
                }
            ]
        };

        var loadMethod = typeof(MainWindowViewModel).GetMethod("LoadReport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.That(loadMethod, Is.Not.Null);
        loadMethod!.Invoke(vm, [report]);

        vm.SearchText = "news";

        Assert.Multiple(() =>
        {
            Assert.That(vm.AllClusters.Count, Is.EqualTo(2));
            Assert.That(vm.Clusters.Count, Is.EqualTo(1));
            Assert.That(vm.SelectedCluster, Is.Not.Null);
            Assert.That(vm.SenderRows.Count, Is.EqualTo(1));
        });

        vm.ClearFilterCommand.Execute(null);
        Assert.Multiple(() =>
        {
            Assert.That(vm.MarkedClusterCount, Is.EqualTo(0));
            Assert.That(vm.ReceivedWithinDaysFilter, Is.EqualTo(0));
            Assert.That(vm.ReceivedFromDate, Is.Null);
            Assert.That(vm.ReceivedToDate, Is.Null);
        });

        vm.ReceivedWithinDaysFilter = 7;
        Assert.That(vm.Clusters.Count, Is.EqualTo(1));

        vm.ReceivedWithinDaysFilter = 0;
        vm.ReceivedFromDate = DateTime.UtcNow.AddDays(-2).Date;
        vm.ReceivedToDate = DateTime.UtcNow.Date;
        Assert.That(vm.Clusters.Count, Is.EqualTo(1));

        vm.ReceivedFromDate = DateTime.UtcNow.AddDays(-40).Date;
        vm.ReceivedToDate = DateTime.UtcNow.AddDays(-20).Date;
        Assert.That(vm.Clusters.Count, Is.EqualTo(1));

        vm.ReceivedFromDate = DateTime.UtcNow.AddDays(-40).Date;
        vm.ReceivedToDate = DateTime.UtcNow.Date;
        Assert.That(vm.Clusters.Count, Is.EqualTo(2));

        vm.ReceivedFromDate = null;
        vm.ReceivedToDate = null;
        Assert.That(vm.Clusters.Count, Is.EqualTo(2));
    }

    [TestCase("hell", "Hell")]
    [TestCase("light", "Hell")]
    [TestCase("dunkel", "Dunkel")]
    [TestCase("dark", "Dunkel")]
    [TestCase("system", "System")]
    [TestCase("default", "System")]
    [TestCase("unknown", null)]
    [TestCase(null, null)]
    public void MainWindowViewModel_NormalizeThemeMode_HandlesAliases(string? input, string? expected)
    {
        var method = typeof(MainWindowViewModel).GetMethod("NormalizeThemeMode", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var result = (string?)method!.Invoke(null, [input]);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void MainWindowViewModel_BuildConfigArguments_HandlesEmptyAndTrimmed()
    {
        var method = typeof(MainWindowViewModel).GetMethod("BuildConfigArguments", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        var empty = (string[])method!.Invoke(null, [""])!;
        var trimmed = (string[])method.Invoke(null, ["  appsettings.json  "])!;

        Assert.Multiple(() =>
        {
            Assert.That(empty, Is.Empty);
            Assert.That(trimmed, Is.EqualTo(new[] { "--config", "appsettings.json" }));
        });
    }

    [Test]
    public void MainWindowViewModel_CanOpenHtmlViewer_ReflectsBusyAndFileState()
    {
        var vm = new MainWindowViewModel();
        var canOpenMethod = typeof(MainWindowViewModel).GetMethod("CanOpenHtmlViewer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(canOpenMethod, Is.Not.Null);

        var tempDir = CreateTempDirectory();
        try
        {
            var htmlPath = Path.Combine(tempDir, "viewer.html");

            vm.IsBusy = true;
            var whenBusy = (bool)canOpenMethod!.Invoke(vm, null)!;

            vm.IsBusy = false;
            vm.ApplySettings(true, false, 1, 0, true, "appsettings.json", "", "google-client-secret.json", "a.csv", "a.json", htmlPath);
            var whenMissing = (bool)canOpenMethod.Invoke(vm, null)!;

            File.WriteAllText(htmlPath, "<html></html>");
            var whenExists = (bool)canOpenMethod.Invoke(vm, null)!;

            Assert.Multiple(() =>
            {
                Assert.That(whenBusy, Is.False);
                Assert.That(whenMissing, Is.False);
                Assert.That(whenExists, Is.True);
            });
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "mailgrabber-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
