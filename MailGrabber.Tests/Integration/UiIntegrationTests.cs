using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using MailGrabber.Behaviors;
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
    public void TextBoxBehaviors_AutoScrollToEnd_MovesCaretWhenTextChanges()
    {
        var textBox = new TextBox();
        TextBoxBehaviors.SetAutoScrollToEnd(textBox, true);
        textBox.Text = "line1\nline2";

        Assert.That(textBox.CaretIndex, Is.EqualTo(textBox.Text!.Length));
    }

    [Test]
    public void MainWindowViewModel_ToggleSelectedFilterClusterCommand_TogglesSelection()
    {
        var vm = new MainWindowViewModel();

        var bucket = new Models.ClusterBucket
        {
            Cluster = "integration",
            SenderCount = 1,
            MessageCount = 1
        };

        var selected = new SelectableClusterViewModel(bucket, () => { });
        vm.SelectedFilterCluster = selected;
        vm.ToggleSelectedFilterClusterCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(selected.IsSelected, Is.True);
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
