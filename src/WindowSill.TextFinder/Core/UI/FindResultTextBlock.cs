using System.ComponentModel;

namespace WindowSill.TextFinder.Core.UI;

/// <summary>
/// A custom control that displays text with a highlighted span,
/// useful for showing search result previews with the match emphasized.
/// </summary>
internal sealed class FindResultTextBlock : Control
{
    private RichTextBlock? _textBlock;

    public FindResultTextBlock()
    {
        DefaultStyleKey = typeof(FindResultTextBlock);
    }

    public static readonly DependencyProperty ResultProperty =
        DependencyProperty.Register(
            nameof(Result),
            typeof(FindResult),
            typeof(FindResultTextBlock),
            new PropertyMetadata(null, OnResultChanged));

    /// <summary>
    /// Gets or sets the search result to display with highlighting.
    /// </summary>
    public FindResult? Result
    {
        get => (FindResult?)GetValue(ResultProperty);
        set => SetValue(ResultProperty, value);
    }

    public static readonly DependencyProperty HighlightBrushProperty =
        DependencyProperty.Register(
            nameof(HighlightBrush),
            typeof(SolidColorBrush),
            typeof(FindResultTextBlock),
            new PropertyMetadata(new SolidColorBrush(Colors.SaddleBrown), OnHighlightBrushChanged));

    /// <summary>
    /// Gets or sets the brush used to highlight the match.
    /// </summary>
    public SolidColorBrush HighlightBrush
    {
        get => (SolidColorBrush)GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _textBlock = GetTemplateChild("PART_TextBlock") as RichTextBlock;
        UpdateHighlighting();
    }

    private static void OnResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FindResultTextBlock control)
        {
            if (e.OldValue is FindResult oldResult)
            {
                oldResult.PropertyChanged -= control.OnResultPropertyChanged;
            }

            if (e.NewValue is FindResult newResult)
            {
                newResult.PropertyChanged += control.OnResultPropertyChanged;
            }

            control.UpdateHighlighting();
        }
    }

    private void OnResultPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateHighlighting();
    }

    private static void OnHighlightBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FindResultTextBlock control)
        {
            control.UpdateHighlighting();
        }
    }

    private void UpdateHighlighting()
    {
        if (_textBlock is null)
        {
            return;
        }

        _textBlock.Blocks.Clear();

        _textBlock.TextHighlighters.Clear();

        FindResult? result = Result;
        if (result is null || string.IsNullOrEmpty(result.PreviewText))
        {
            return;
        }

        string text = result.PreviewText;
        TextSpan matchSpan = result.MatchInPreview;

        var paragraph = new Paragraph();
        paragraph.Inlines.Add(new Run { Text = text });
        _textBlock.Blocks.Add(paragraph);

        int matchStart = matchSpan.Index;
        int matchLength = matchSpan.Length;

        // Clamp values to valid range
        matchStart = Math.Max(0, Math.Min(matchStart, text.Length));
        matchLength = Math.Max(0, Math.Min(matchLength, text.Length - matchStart));

        if (matchLength > 0)
        {
            var highlighter = new TextHighlighter
            {
                Background = HighlightBrush,
                Foreground = Foreground
            };
            highlighter.Ranges.Add(new TextRange(matchStart, matchLength));
            _textBlock.TextHighlighters.Add(highlighter);
        }
    }
}
