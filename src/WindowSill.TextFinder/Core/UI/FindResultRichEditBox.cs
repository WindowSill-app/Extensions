using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Text;

namespace WindowSill.TextFinder.Core.UI;

/// <summary>
/// A <see cref="RichEditBox"/> that supports highlighting search matches
/// with configurable colors for all matches and the selected match.
/// </summary>
internal sealed class FindResultRichEditBox : RichEditBox
{
    private bool _isUpdating;
    private ScrollViewer? _scrollViewer;

    public static readonly DependencyProperty PlainTextProperty =
        DependencyProperty.Register(
            nameof(PlainText),
            typeof(string),
            typeof(FindResultRichEditBox),
            new PropertyMetadata(string.Empty, OnHighlightingPropertyChanged));

    /// <summary>
    /// Gets or sets the plain text content to display.
    /// </summary>
    public string PlainText
    {
        get => (string)GetValue(PlainTextProperty);
        set => SetValue(PlainTextProperty, value);
    }

    public static readonly DependencyProperty HighlightResultsProperty =
        DependencyProperty.Register(
            nameof(HighlightResults),
            typeof(ObservableCollection<FindResult>),
            typeof(FindResultRichEditBox),
            new PropertyMetadata(null, OnHighlightResultsChanged));

    /// <summary>
    /// Gets or sets the collection of search results to highlight.
    /// </summary>
    public ObservableCollection<FindResult>? HighlightResults
    {
        get => (ObservableCollection<FindResult>?)GetValue(HighlightResultsProperty);
        set => SetValue(HighlightResultsProperty, value);
    }

    public static readonly DependencyProperty SelectedMatchProperty =
        DependencyProperty.Register(
            nameof(SelectedMatch),
            typeof(FindResult),
            typeof(FindResultRichEditBox),
            new PropertyMetadata(null, OnHighlightingPropertyChanged));

    /// <summary>
    /// Gets or sets the currently selected match, highlighted with a distinct color.
    /// </summary>
    public FindResult? SelectedMatch
    {
        get => (FindResult?)GetValue(SelectedMatchProperty);
        set => SetValue(SelectedMatchProperty, value);
    }

    public static readonly DependencyProperty HighlightBrushProperty =
        DependencyProperty.Register(
            nameof(HighlightBrush),
            typeof(SolidColorBrush),
            typeof(FindResultRichEditBox),
            new PropertyMetadata(new SolidColorBrush(Colors.SaddleBrown), OnHighlightingPropertyChanged));

    /// <summary>
    /// Gets or sets the background brush for all highlighted matches.
    /// </summary>
    public SolidColorBrush HighlightBrush
    {
        get => (SolidColorBrush)GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    public static readonly DependencyProperty SelectedHighlightBrushProperty =
        DependencyProperty.Register(
            nameof(SelectedHighlightBrush),
            typeof(SolidColorBrush),
            typeof(FindResultRichEditBox),
            new PropertyMetadata(new SolidColorBrush(Colors.Orange), OnHighlightingPropertyChanged));

    /// <summary>
    /// Gets or sets the background brush for the selected match.
    /// </summary>
    public SolidColorBrush SelectedHighlightBrush
    {
        get => (SolidColorBrush)GetValue(SelectedHighlightBrushProperty);
        set => SetValue(SelectedHighlightBrushProperty, value);
    }

    public FindResultRichEditBox()
    {
        Style = (Style)Application.Current.Resources["DefaultRichEditBoxStyle"];
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshHighlighting();
    }

    private static void OnHighlightResultsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (FindResultRichEditBox)d;

        if (e.OldValue is ObservableCollection<FindResult> oldCollection)
        {
            oldCollection.CollectionChanged -= control.OnHighlightResultsCollectionChanged;
        }

        if (e.NewValue is ObservableCollection<FindResult> newCollection)
        {
            newCollection.CollectionChanged += control.OnHighlightResultsCollectionChanged;
        }

        control.RefreshHighlighting();
    }

    private static void OnHighlightingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((FindResultRichEditBox)d).RefreshHighlighting();
    }

    private void OnHighlightResultsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshHighlighting();
    }

    private void RefreshHighlighting()
    {
        if (_isUpdating || !IsLoaded)
        {
            return;
        }

        _isUpdating = true;
        bool wasReadOnly = IsReadOnly;

        try
        {
            IsReadOnly = false;
            Document.SetText(TextSetOptions.None, PlainText ?? string.Empty);

            // Clear existing highlighting.
            ITextRange fullRange = Document.GetRange(0, TextConstants.MaxUnitCount);
            fullRange.CharacterFormat.BackgroundColor = Colors.Transparent;

            if (HighlightResults is null || HighlightResults.Count == 0)
            {
                return;
            }

            foreach (FindResult result in HighlightResults)
            {
                ApplyHighlight(result.Match, HighlightBrush.Color);
            }

            if (SelectedMatch is not null)
            {
                ApplyHighlight(SelectedMatch.Match, SelectedHighlightBrush.Color);
                ScrollToMatch(SelectedMatch.Match);
            }
        }
        finally
        {
            IsReadOnly = wasReadOnly;
            _isUpdating = false;
        }
    }

    private void ScrollToMatch(TextSpan span)
    {
        ITextRange range = Document.GetRange(span.Index, span.Index + span.Length);

        _scrollViewer ??= FindVisualChild<ScrollViewer>(this);
        if (_scrollViewer is null)
        {
            range.ScrollIntoView(PointOptions.Start);
            return;
        }

        range.GetRect(PointOptions.None, out Windows.Foundation.Rect rect, out _);

        double viewportTop = _scrollViewer.VerticalOffset;
        double viewportBottom = viewportTop + _scrollViewer.ViewportHeight;

        bool isVerticallyVisible = rect.Top >= viewportTop && rect.Bottom <= viewportBottom;

        if (!isVerticallyVisible)
        {
            range.ScrollIntoView(PointOptions.Start);
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
            {
                return found;
            }

            T? result = FindVisualChild<T>(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private void ApplyHighlight(TextSpan span, Windows.UI.Color backgroundColor)
    {
        ITextRange range = Document.GetRange(span.Index, span.Index + span.Length);
        range.CharacterFormat.BackgroundColor = backgroundColor;
    }
}
