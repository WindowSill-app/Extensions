using LiveChartsCore.Measure;

using WindowSill.API;
using WindowSill.PerfCounter.ViewModels;

namespace WindowSill.PerfCounter.Views;

/// <summary>
/// Popup content view displaying real-time CPU, Memory, and GPU usage charts.
/// </summary>
internal sealed partial class PerfCounterPopupContent : SillPopupContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PerfCounterPopupContent"/> class.
    /// </summary>
    /// <param name="viewModel">The ViewModel providing chart data.</param>
    internal PerfCounterPopupContent(PerfCounterPopupViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the ViewModel for data binding.
    /// </summary>
    internal PerfCounterPopupViewModel ViewModel { get; }

    /// <summary>
    /// Gets the chart draw margin to make charts edge-to-edge within cards.
    /// </summary>
    internal Margin ChartDrawMargin { get; } = new(0, 0, 0, 0);
}
