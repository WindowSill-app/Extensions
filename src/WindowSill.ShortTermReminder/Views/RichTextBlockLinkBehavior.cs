using System.Windows.Input;

using Windows.System;

using WindowSill.ShortTermReminder.Core;

namespace WindowSill.ShortTermReminder.Views;

/// <summary>
/// Attached behavior for <see cref="RichTextBlock"/> that parses text containing URIs
/// and renders them as clickable <see cref="Hyperlink"/> inlines.
/// </summary>
internal static class RichTextBlockLinkBehavior
{
    /// <summary>
    /// Identifies the <c>LinkedText</c> attached dependency property.
    /// When set on a <see cref="RichTextBlock"/>, the text is parsed for URIs
    /// and rendered with <see cref="Hyperlink"/> inlines.
    /// </summary>
    internal static readonly DependencyProperty LinkedTextProperty =
        DependencyProperty.RegisterAttached(
            "LinkedText",
            typeof(string),
            typeof(RichTextBlockLinkBehavior),
            new PropertyMetadata(null, OnLinkedTextChanged));

    /// <summary>
    /// Identifies the <c>LinkClickedCommand</c> attached dependency property.
    /// An optional <see cref="ICommand"/> invoked with the clicked <see cref="Uri"/>
    /// as parameter. If not set, the default behavior launches the URI in the default browser.
    /// </summary>
    internal static readonly DependencyProperty LinkClickedCommandProperty =
        DependencyProperty.RegisterAttached(
            "LinkClickedCommand",
            typeof(ICommand),
            typeof(RichTextBlockLinkBehavior),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the <c>LinkedText</c> attached property value.
    /// </summary>
    /// <param name="obj">The target <see cref="RichTextBlock"/>.</param>
    /// <returns>The linked text string.</returns>
    internal static string GetLinkedText(DependencyObject obj)
        => (string)obj.GetValue(LinkedTextProperty);

    /// <summary>
    /// Sets the <c>LinkedText</c> attached property value.
    /// </summary>
    /// <param name="obj">The target <see cref="RichTextBlock"/>.</param>
    /// <param name="value">The text to parse and render with URI links.</param>
    internal static void SetLinkedText(DependencyObject obj, string value)
        => obj.SetValue(LinkedTextProperty, value);

    /// <summary>
    /// Gets the <c>LinkClickedCommand</c> attached property value.
    /// </summary>
    /// <param name="obj">The target <see cref="RichTextBlock"/>.</param>
    /// <returns>The command to invoke when a link is clicked.</returns>
    internal static ICommand GetLinkClickedCommand(DependencyObject obj)
        => (ICommand)obj.GetValue(LinkClickedCommandProperty);

    /// <summary>
    /// Sets the <c>LinkClickedCommand</c> attached property value.
    /// </summary>
    /// <param name="obj">The target <see cref="RichTextBlock"/>.</param>
    /// <param name="value">The command to invoke when a link is clicked.</param>
    internal static void SetLinkClickedCommand(DependencyObject obj, ICommand value)
        => obj.SetValue(LinkClickedCommandProperty, value);

    private static void OnLinkedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBlock richTextBlock)
        {
            return;
        }

        richTextBlock.Blocks.Clear();

        string? text = e.NewValue as string;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        IReadOnlyList<TextSegment> segments = UriHelper.GetTextSegments(text);
        var paragraph = new Paragraph();

        foreach (TextSegment segment in segments)
        {
            if (segment.IsUri)
            {
                var hyperlink = new Hyperlink();
                hyperlink.Inlines.Add(new Run { Text = segment.Text });
                hyperlink.Click += (sender, args) => OnHyperlinkClicked(richTextBlock, segment.Uri!);
                paragraph.Inlines.Add(hyperlink);
            }
            else
            {
                paragraph.Inlines.Add(new Run { Text = segment.Text });
            }
        }

        richTextBlock.Blocks.Add(paragraph);
    }

    private static void OnHyperlinkClicked(RichTextBlock richTextBlock, Uri uri)
    {
        var command = richTextBlock.GetValue(LinkClickedCommandProperty) as ICommand;

        if (command is not null && command.CanExecute(uri))
        {
            command.Execute(uri);
        }
        else
        {
            _ = Launcher.LaunchUriAsync(uri);
        }
    }
}
