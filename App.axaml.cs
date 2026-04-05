using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MailGrabber.Services;
using MailGrabber.ViewModels;
using MailGrabber.Views;
using System.Diagnostics.CodeAnalysis;

namespace MailGrabber;

[ExcludeFromCodeCoverage]
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainWindowViewModel(new MainWindowDialogService(mainWindow));
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
