using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using WindowSill.API;

namespace WindowSill.URLHelper.Core;

/// <summary>
/// Extracts application icons from executable files using the Windows Storage thumbnail API.
/// </summary>
internal static class BrowserIconExtractor
{
    private const uint IconSize = 16;

    private static readonly ILogger _logger = typeof(BrowserIconExtractor).Log();

    /// <summary>
    /// Extracts the icon from the specified executable and returns it as an <see cref="ImageSource"/>.
    /// Returns <c>null</c> if extraction fails.
    /// </summary>
    internal static async Task<ImageSource?> GetIconForExeAsync(string exePath)
    {
        try
        {
            if (!File.Exists(exePath))
            {
                return null;
            }

            StorageFile file = await StorageFile.GetFileFromPathAsync(exePath);
            StorageItemThumbnail? thumbnail = await file.GetThumbnailAsync(
                ThumbnailMode.SingleItem,
                IconSize,
                ThumbnailOptions.UseCurrentScale);

            if (thumbnail is null)
            {
                return null;
            }

            using (thumbnail)
            {
                var bitmap = new WriteableBitmap(
                    (int)thumbnail.OriginalWidth,
                    (int)thumbnail.OriginalHeight);
                await bitmap.SetSourceAsync(thumbnail);
                return bitmap;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract icon from '{ExePath}'.", exePath);
            return null;
        }
    }
}
