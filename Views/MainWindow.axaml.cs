using Avalonia.Controls;
using System.Diagnostics.CodeAnalysis;

namespace MailGrabber.Views;

[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnRunLogTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        textBox.CaretIndex = textBox.Text?.Length ?? 0;
    }
}

