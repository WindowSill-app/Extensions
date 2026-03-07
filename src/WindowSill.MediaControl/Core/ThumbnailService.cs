using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

using WindowSill.API;

namespace WindowSill.MediaControl.Core;

/// <inheritdoc />
internal sealed class ThumbnailService : IThumbnailService
{
    private const uint HorizontalThumbnailSize = 40;
    private const uint VerticalThumbnailSize = 64;

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailService"/> class.
    /// </summary>
    public ThumbnailService()
    {
        _logger = this.Log();
    }

    /// <inheritdoc />
    public async Task<(ImageSource? Thumbnail, ImageSource? ThumbnailLarge)> CreateThumbnailsAsync(
        Stream? thumbnailStream,
        SillLocation sillLocation)
    {
        try
        {
            if (thumbnailStream is not null)
            {
                using IRandomAccessStream stream = thumbnailStream.AsRandomAccessStream();

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                SoftwareBitmap thumbnailLarge = await decoder.GetSoftwareBitmapAsync();
                if (thumbnailLarge.BitmapPixelFormat != BitmapPixelFormat.Bgra8
                    || thumbnailLarge.BitmapAlphaMode == BitmapAlphaMode.Straight)
                {
                    thumbnailLarge = SoftwareBitmap.Convert(
                        thumbnailLarge,
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied);
                }

                uint size = sillLocation is SillLocation.Left or SillLocation.Right
                    ? VerticalThumbnailSize
                    : HorizontalThumbnailSize;

                var transform = new BitmapTransform()
                {
                    ScaledWidth = size,
                    ScaledHeight = size,
                    InterpolationMode = BitmapInterpolationMode.Fant,
                };

                SoftwareBitmap thumbnailSmall = await decoder.GetSoftwareBitmapAsync(
                    thumbnailLarge.BitmapPixelFormat,
                    thumbnailLarge.BitmapAlphaMode,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.ColorManageToSRgb);

                var sourceLarge = new SoftwareBitmapSource();
                await sourceLarge.SetBitmapAsync(thumbnailLarge);

                var sourceSmall = new SoftwareBitmapSource();
                await sourceSmall.SetBitmapAsync(thumbnailSmall);

                return (sourceSmall, sourceLarge);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to retrieve media thumbnail.");
        }
        finally
        {
            thumbnailStream?.Dispose();
        }

        return (null, null);
    }
}
