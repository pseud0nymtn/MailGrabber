using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using MailGrabber.ViewModels;
using MailGrabber.Views;

namespace MailGrabber.Tests.Integration;

[Category("Integration")]
[NonParallelizable]
public class UiIntegrationTests
{
    [OneTimeSetUp]
    public void SetupAvalonia()
    {
        if (Application.Current is null)
        {
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }
    }

    [Test]
    public void MainWindow_CanBeConstructed()
    {
        var window = new MainWindow();

        Assert.That(window, Is.Not.Null);
        Assert.That(window.DataContext, Is.Null);
    }

    [Test]
    public void SettingsWindow_CanBeConstructed()
    {
        var window = new SettingsWindow();

        Assert.That(window, Is.Not.Null);
        Assert.That(window.DataContext, Is.Null);
    }

    [Test]
    public void MainWindow_OnRunLogTextChanged_MovesCaretToEnd()
    {
        var window = new MainWindow();
        var method = typeof(MainWindow).GetMethod("OnRunLogTextChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null);

        var textBox = new TextBox { Text = "line1\nline2" };
        method!.Invoke(window, [textBox, null]);

        Assert.That(textBox.CaretIndex, Is.EqualTo(textBox.Text!.Length));
    }

    [Test]
    public void MainWindow_OnClusterFilterListKeyDown_HandlesSpaceForSelectedCluster()
    {
        var window = new MainWindow();
        var vm = new MainWindowViewModel();
        window.DataContext = vm;

        var bucket = new Models.ClusterBucket
        {
            Cluster = "integration",
            SenderCount = 1,
            MessageCount = 1
        };

        var selected = new SelectableClusterViewModel(bucket, () => { });
        vm.SelectedFilterCluster = selected;

        var method = typeof(MainWindow).GetMethod("OnClusterFilterListKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(method, Is.Not.Null);

        var args = new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
            Key = Avalonia.Input.Key.Space
        };

        method!.Invoke(window, [null, args]);

        Assert.Multiple(() =>
        {
            Assert.That(selected.IsSelected, Is.True);
            Assert.That(args.Handled, Is.True);
        });
    }

    [Test]
    public void SettingsWindow_PrivateHelpers_ReturnSafelyInNonDialogContext()
    {
        var window = new SettingsWindow();

        var applyMethod = typeof(SettingsWindow).GetMethod("ApplyMaxHeightFromCurrentScreen", BindingFlags.NonPublic | BindingFlags.Instance);
        var saveAsMethod = typeof(SettingsWindow).GetMethod("OnSaveAsButtonClick", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.That(applyMethod, Is.Not.Null);
        Assert.That(saveAsMethod, Is.Not.Null);

        Assert.DoesNotThrow(() => applyMethod!.Invoke(window, null));
        Assert.DoesNotThrow(() => saveAsMethod!.Invoke(window, [null, null]));
    }
}
