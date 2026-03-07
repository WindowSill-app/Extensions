using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;
using WindowSill.ClipboardHistory.Utils;

namespace WindowSill.ClipboardHistory.ViewModels;

/// <summary>
/// ViewModel for clipboard history items containing image data.
/// </summary>
internal sealed partial class ImageItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageItemViewModel"/> class.
    /// </summary>
    /// <param name="processInteractionService">Service for interacting with external processes.</param>
    /// <param name="item">The clipboard history item containing image data.</param>
    internal ImageItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _logger = this.Log();
        InitializeAsync().Forget();
    }

    [ObservableProperty]
    public partial string Size { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double MaxHeight { get; set; }

    [ObservableProperty]
    public partial double MaxWidth { get; set; }

    [ObservableProperty]
    public partial BitmapImage? Image { get; set; }

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);
            BitmapImage? bitmap = await DataHelper.GetBitmapAsync(Data);
            Image = bitmap;

            if (bitmap is not null)
            {
                MaxHeight = bitmap.PixelHeight;
                MaxWidth = bitmap.PixelWidth;
                Image = bitmap;
                Size
                    = string.Format(
                        "/WindowSill.ClipboardHistory/Misc/ImageSize".GetLocalizedString(),
                        bitmap.PixelWidth,
                        bitmap.PixelHeight);
            }
            else
            {
                // TODO: Show that we can't preview the image.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(ImageItemViewModel)} control.");
        }
    }
}
