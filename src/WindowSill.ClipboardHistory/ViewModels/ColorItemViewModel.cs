using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;
using WindowSill.ClipboardHistory.Utils;

namespace WindowSill.ClipboardHistory.ViewModels;

/// <summary>
/// ViewModel for clipboard history items containing color data.
/// </summary>
internal sealed partial class ColorItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorItemViewModel"/> class.
    /// </summary>
    /// <param name="processInteractionService">Service for interacting with external processes.</param>
    /// <param name="item">The clipboard history item containing color data.</param>
    internal ColorItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _logger = this.Log();
        InitializeAsync().Forget();
    }

    [ObservableProperty]
    public partial string ColorText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SolidColorBrush? BackgroundBrush { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush? ForegroundBrush { get; set; }

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);
            string colorString = await Data.GetTextAsync();
            ColorText = colorString;

            (SolidColorBrush background, SolidColorBrush foreground) = DataHelper.GetBackgroundAndForegroundBrushes(colorString);
            BackgroundBrush = background;
            ForegroundBrush = foreground;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(ColorItemViewModel)} control.");
        }
    }
}
