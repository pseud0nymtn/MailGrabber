using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace MailGrabber.Behaviors;

public sealed class TextBoxBehaviors
{
    public static readonly AttachedProperty<bool> AutoScrollToEndProperty =
        AvaloniaProperty.RegisterAttached<TextBoxBehaviors, TextBox, bool>("AutoScrollToEnd");

    static TextBoxBehaviors()
    {
        AutoScrollToEndProperty.Changed.AddClassHandler<TextBox>(OnAutoScrollToEndChanged);
        TextBox.TextProperty.Changed.AddClassHandler<TextBox>(OnTextPropertyChanged);
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
            MoveCaretToEnd(textBox);
            return;
        }
    }

    private static void OnTextPropertyChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs args)
    {
        if (GetAutoScrollToEnd(textBox))
        {
            MoveCaretToEnd(textBox);
        }
    }

    private static void MoveCaretToEnd(TextBox textBox)
    {
        var index = textBox.Text?.Length ?? 0;

        // Do one immediate update and one deferred update after layout,
        // so multiline read-only boxes reliably land on the real last line.
        MoveCaretAndScroll(textBox, index);
        Dispatcher.UIThread.Post(() => MoveCaretAndScroll(textBox, index), DispatcherPriority.Background);
    }

    private static void MoveCaretAndScroll(TextBox textBox, int index)
    {
        textBox.CaretIndex = index;
        textBox.SelectionStart = index;
        textBox.SelectionEnd = index;
    }
}
