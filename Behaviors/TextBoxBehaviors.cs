using Avalonia;
using Avalonia.Controls;

namespace MailGrabber.Behaviors;

public sealed class TextBoxBehaviors
{
    public static readonly AttachedProperty<bool> AutoScrollToEndProperty =
        AvaloniaProperty.RegisterAttached<TextBoxBehaviors, TextBox, bool>("AutoScrollToEnd");

    static TextBoxBehaviors()
    {
        AutoScrollToEndProperty.Changed.AddClassHandler<TextBox>(OnAutoScrollToEndChanged);
    }

    public static bool GetAutoScrollToEnd(TextBox textBox)
    {
        return textBox.GetValue(AutoScrollToEndProperty);
    }

    public static void SetAutoScrollToEnd(TextBox textBox, bool value)
    {
        textBox.SetValue(AutoScrollToEndProperty, value);
    }

    private static void OnAutoScrollToEndChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not bool isEnabled)
        {
            return;
        }

        if (isEnabled)
        {
            textBox.TextChanged += OnTextChanged;
            MoveCaretToEnd(textBox);
            return;
        }

        textBox.TextChanged -= OnTextChanged;
    }

    private static void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            MoveCaretToEnd(textBox);
        }
    }

    private static void MoveCaretToEnd(TextBox textBox)
    {
        textBox.CaretIndex = textBox.Text?.Length ?? 0;
    }
}
