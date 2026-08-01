using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using WindowSill.API;
using WindowSill.FileHelper.ViewModels;

namespace WindowSill.FileHelper.Views;

/// <summary>
/// Popup content shown when the ZIP summary sill item is clicked. It repeats the compact headline (compression
/// outcome, counts, sizes) and lists every file in the archive, largest-first, with each file's before/after sizes
/// and its individual compression outcome. It is read-only: the same single central-directory inspection that backs
/// the inline summary supplies the data, so opening the popup never re-reads or extracts the archive.
/// </summary>
/// <remarks>
/// The header is an acrylic overlay drawn on top of the entry list: entries scroll up behind it and show through
/// its translucency. As the list scrolls, the header collapses from its full height down to just the archive file
/// name, and its detail lines (compression outcome, counts, sizes) fade out. The collapse is driven directly from
/// the list's inner <see cref="ScrollViewer"/> vertical offset.
/// </remarks>
internal sealed partial class ZipDetailsPopup : SillPopupContent
{
    /// <summary>The header's full height, in DIPs, when the list is scrolled to the top.</summary>
    private const double ExpandedHeaderHeight = 96d;

    /// <summary>The header's collapsed height, in DIPs, once the list has scrolled past the fade range.</summary>
    private const double CollapsedHeaderHeight = 48d;

    /// <summary>
    /// The scroll distance, in DIPs, over which the header collapses and its detail lines fade. Kept equal to the
    /// height delta so the header collapses exactly in step with the scroll: when the list has scrolled by this
    /// amount, the first entry sits flush against the collapsed header with no gap.
    /// </summary>
    private const double CollapseRange = ExpandedHeaderHeight - CollapsedHeaderHeight;

    private ScrollViewer? _scrollViewer;
    private ScrollBar? _verticalScrollBar;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZipDetailsPopup"/> class, bound to the same
    /// <see cref="ZipInfoViewModel"/> instance that backs the inline sill summary.
    /// </summary>
    /// <param name="viewModel">The shared ZIP inspection view model.</param>
    internal ZipDetailsPopup(ZipInfoViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        EntriesList.Loaded += EntriesList_Loaded;
        EntriesList.Unloaded += EntriesList_Unloaded;
        HeaderBorder.SizeChanged += HeaderBorder_SizeChanged;
    }

    /// <summary>
    /// Gets the view model for this popup.
    /// </summary>
    internal ZipInfoViewModel ViewModel { get; }

    /// <summary>
    /// Resolves the themed brush used to color a compression outcome: a success color when the content compressed
    /// smaller, a caution color when it grew larger, and the primary text color otherwise (equal or not applicable).
    /// Exposed as a static so both the header and each list row can bind their outcome color through x:Bind.
    /// </summary>
    /// <param name="isSmaller">Whether the outcome is "smaller than original".</param>
    /// <param name="isLarger">Whether the outcome is "larger than original".</param>
    /// <returns>The brush to apply to the outcome text.</returns>
    public static Brush OutcomeBrush(bool isSmaller, bool isLarger)
    {
        string brushKey = isSmaller
            ? "SystemFillColorSuccessBrush"
            : isLarger
                ? "SystemFillColorCautionBrush"
                : "TextFillColorPrimaryBrush";

        if (Application.Current.Resources.TryGetValue(brushKey, out object? brush) && brush is Brush outcomeBrush)
        {
            return outcomeBrush;
        }

        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private void EntriesList_Loaded(object sender, RoutedEventArgs e) => TryAttachScrollViewer();

    /// <summary>
    /// Clips the header's content to its current bounds so that, as the header shrinks, the fading detail lines are
    /// cut off at the header's edge instead of bleeding down over the list entries behind it.
    /// </summary>
    private void HeaderBorder_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        HeaderBorder.Clip = new Microsoft.UI.Xaml.Media.RectangleGeometry
        {
            Rect = new Windows.Foundation.Rect(0, 0, HeaderBorder.ActualWidth, HeaderBorder.ActualHeight),
        };
    }

    private void EntriesList_Unloaded(object sender, RoutedEventArgs e)
    {
        EntriesList.LayoutUpdated -= EntriesList_LayoutUpdated;

        if (_scrollViewer is not null)
        {
            _scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
            _scrollViewer = null;
        }

        _verticalScrollBar = null;
    }

    private void EntriesList_LayoutUpdated(object? sender, object e) => TryAttachScrollViewer();

    /// <summary>
    /// Finds the list's inner <see cref="ScrollViewer"/> and subscribes to its scroll changes. The template of a
    /// list that starts out collapsed (while inspection is loading) may not be realized until it becomes visible, so
    /// if the scroll viewer isn't available yet we retry on the next layout pass until it is.
    /// </summary>
    private void TryAttachScrollViewer()
    {
        if (_scrollViewer is not null)
        {
            return;
        }

        _scrollViewer = FindDescendant<ScrollViewer>(EntriesList);

        if (_scrollViewer is null)
        {
            // Retry on the next layout pass (e.g. once the list becomes visible and its template is applied).
            EntriesList.LayoutUpdated -= EntriesList_LayoutUpdated;
            EntriesList.LayoutUpdated += EntriesList_LayoutUpdated;
            return;
        }

        EntriesList.LayoutUpdated -= EntriesList_LayoutUpdated;
        _scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

        // The list fills the whole popup so entries can scroll behind the header; that also makes the inner vertical
        // scrollbar span the full height, so its top would sit behind the header. Offset it to start at the header's
        // bottom edge (kept in sync as the header collapses in UpdateHeaderForOffset).
        _verticalScrollBar = FindDescendant<ScrollBar>(_scrollViewer, bar => bar.Orientation == Orientation.Vertical);

        UpdateHeaderForOffset(_scrollViewer.VerticalOffset);
    }

    private void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_scrollViewer is not null)
        {
            UpdateHeaderForOffset(_scrollViewer.VerticalOffset);
        }
    }

    /// <summary>
    /// Collapses the header height and fades its detail lines in proportion to how far the list has scrolled: the
    /// header shrinks 1:1 with scrolling until it reaches its collapsed height, and the detail lines fade fully out
    /// over the same range, leaving only the archive file name.
    /// </summary>
    private void UpdateHeaderForOffset(double verticalOffset)
    {
        double clampedOffset = Math.Clamp(verticalOffset, 0d, CollapseRange);

        double headerHeight = ExpandedHeaderHeight - clampedOffset;
        HeaderBorder.Height = headerHeight;
        HeaderDetails.Opacity = 1d - (clampedOffset / CollapseRange);

        // Keep the scrollbar's top flush with the header's bottom edge as the header collapses, so it's never hidden
        // behind the header and never leaves a gap below it.
        if (_verticalScrollBar is not null)
        {
            _verticalScrollBar.Margin = new Thickness(0, headerHeight, 0, 0);
        }
    }

    /// <summary>
    /// Depth-first searches the visual tree under <paramref name="root"/> for the first descendant of type
    /// <typeparamref name="T"/> that satisfies <paramref name="predicate"/> (or any such descendant when no predicate
    /// is supplied).
    /// </summary>
    private static T? FindDescendant<T>(DependencyObject root, Func<T, bool>? predicate = null)
        where T : DependencyObject
    {
        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild && (predicate is null || predicate(typedChild)))
            {
                return typedChild;
            }

            T? descendant = FindDescendant(child, predicate);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
