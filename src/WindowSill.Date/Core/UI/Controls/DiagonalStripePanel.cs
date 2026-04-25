using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace WindowSill.Date.Core.UI.Controls;

/// <summary>
/// A lightweight panel that draws diagonal stripes using solid line geometry.
/// Avoids <see cref="LinearGradientBrush"/> with <c>SpreadMethod="Repeat"</c>,
/// which causes WinUI 3 compositor alpha corruption on sibling elements.
/// </summary>
internal sealed class DiagonalStripePanel : Canvas
{
    /// <summary>
    /// Identifies the <see cref="StrokeBrush"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty StrokeBrushProperty =
        DependencyProperty.Register(
            nameof(StrokeBrush),
            typeof(Brush),
            typeof(DiagonalStripePanel),
            new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="Spacing"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(
            nameof(Spacing),
            typeof(double),
            typeof(DiagonalStripePanel),
            new PropertyMetadata(4.0, OnPropertyChanged));

    /// <summary>
    /// Identifies the <see cref="StrokeWidth"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty StrokeWidthProperty =
        DependencyProperty.Register(
            nameof(StrokeWidth),
            typeof(double),
            typeof(DiagonalStripePanel),
            new PropertyMetadata(1.5, OnPropertyChanged));

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagonalStripePanel"/> class.
    /// </summary>
    public DiagonalStripePanel()
    {
        SizeChanged += (_, e) => Redraw(e.NewSize);
    }

    /// <summary>
    /// Gets or sets the brush used to stroke the diagonal lines.
    /// </summary>
    public Brush? StrokeBrush
    {
        get => (Brush?)GetValue(StrokeBrushProperty);
        set => SetValue(StrokeBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the spacing between stripe lines in pixels.
    /// </summary>
    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the stroke thickness of each stripe line.
    /// </summary>
    public double StrokeWidth
    {
        get => (double)GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DiagonalStripePanel panel && panel.ActualWidth > 0 && panel.ActualHeight > 0)
        {
            panel.Redraw(new Windows.Foundation.Size(panel.ActualWidth, panel.ActualHeight));
        }
    }

    private void Redraw(Windows.Foundation.Size size)
    {
        Children.Clear();

        if (StrokeBrush is null || size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        double w = size.Width;
        double h = size.Height;
        double spacing = Spacing;

        // Build a single Path with all diagonal line segments.
        var geometry = new PathGeometry();
        for (double y = -w; y <= h + w; y += spacing)
        {
            var figure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(0, y + w),
                IsClosed = false,
            };
            figure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(w, y) });
            geometry.Figures.Add(figure);
        }

        Children.Add(new Path
        {
            Data = geometry,
            Stroke = StrokeBrush,
            StrokeThickness = StrokeWidth,
        });
    }
}
