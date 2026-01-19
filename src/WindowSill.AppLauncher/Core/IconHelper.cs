using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using WindowSill.API;

namespace WindowSill.AppLauncher.Core;

internal static class IconHelper
{
    internal const uint DefaultIconSize = 64;

    internal static async Task<ImageSource?> GetIconFromFileOrFolderAsync(string? filePath, uint size = DefaultIconSize)
    {
        return await ThreadHelper.RunOnUIThreadAsync(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(filePath) || (!File.Exists(filePath) && !Directory.Exists(filePath)))
                    {
                        return null;
                    }

                    StorageItemThumbnail? thumbnail = null;

                    if (File.Exists(filePath))
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
                        thumbnail = await file.GetThumbnailAsync(
                            ThumbnailMode.SingleItem,
                            size,
                            ThumbnailOptions.UseCurrentScale);
                    }
                    else if (Directory.Exists(filePath))
                    {
                        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(filePath);
                        thumbnail = await folder.GetThumbnailAsync(
                            ThumbnailMode.SingleItem,
                            size,
                            ThumbnailOptions.UseCurrentScale);
                    }

                    if (thumbnail == null)
                    {
                        return null;
                    }

                    using (thumbnail)
                    {
                        // Create WriteableBitmap from thumbnail
                        var writeableBitmap = new WriteableBitmap(
                            (int)thumbnail.OriginalWidth,
                            (int)thumbnail.OriginalHeight);

                        await writeableBitmap.SetSourceAsync(thumbnail);

                        return writeableBitmap;
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is COMException)
                {
                    return null;
                }
                catch (Exception ex)
                {
                    typeof(IconHelper).Log().LogError(ex, "Error while retrieving icon for: {filePath}", filePath);
                    return null;
                }
            });
    }

    public static async Task<ImageSource?> CreateGridIconAsync(
        IReadOnlyList<AppInfo.AppInfo> selectedItems,
        int selectedSize)
    {
        Guard.IsNotNull(selectedItems);

        try
        {
            selectedItems = selectedItems.Take(selectedSize * selectedSize).ToList();

            int finalSize = 256;
            int gridSize;
            int cellSize;
            if (selectedItems.Count == 2)
            {
                gridSize = 2;
                cellSize = finalSize / 2;
            }
            else
            {
                gridSize = (int)Math.Ceiling(Math.Sqrt(selectedItems.Count));
                cellSize = finalSize / gridSize;
            }

            // Get shared Win2D device (GPU-accelerated)
            var device = CanvasDevice.GetSharedDevice();

            // Create offscreen render target (GPU memory)
            using var renderTarget = new CanvasRenderTarget(device, finalSize, finalSize, 96);
            using (CanvasDrawingSession drawingSession = renderTarget.CreateDrawingSession())
            {
                // Clear to transparent
                drawingSession.Clear(Colors.Transparent);

                // Composite icons
                for (int i = 0; i < selectedItems.Count; i++)
                {
                    AppInfo.AppInfo item = selectedItems[i];

                    int x, y;
                    if (selectedItems.Count == 2)
                    {
                        if (i == 0)
                        {
                            x = 0;
                            y = cellSize;
                        }
                        else
                        {
                            x = cellSize;
                            y = 0;
                        }
                    }
                    else
                    {
                        int row = i / gridSize;
                        int col = i % gridSize;
                        x = col * cellSize;
                        y = row * cellSize;
                    }

                    if (item.AppIcon.Task is null)
                    {
                        item.AppIcon.Reset();
                    }

                    if (item.AppIcon.Task is not null)
                    {
                        ImageSource? iconImageSource = await item.AppIcon.Task;
                        CanvasBitmap? iconBitmap = await iconImageSource.ToCanvasBitmapAsync(device);

                        if (iconBitmap != null)
                        {
                            using (iconBitmap)
                            {
                                try
                                {
                                    int padding = 5;
                                    int drawSize = cellSize - (padding * 2);

                                    // Hardware-accelerated scaling + alpha blending
                                    var destRect
                                        = new Windows.Foundation.Rect(
                                            x + padding,
                                            y + padding,
                                            drawSize,
                                            drawSize);

                                    drawingSession.DrawImage(iconBitmap, destRect);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error processing icon {i}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }

            // Convert to in-memory stream
            using var memoryStream = new InMemoryRandomAccessStream();
            await renderTarget.SaveAsync(memoryStream, CanvasBitmapFileFormat.Png);
            memoryStream.Seek(0);

            // Load directly into BitmapImage from memory
            var gridIcon = new BitmapImage();
            await gridIcon.SetSourceAsync(memoryStream);

            return gridIcon;

            // Return base64 data URI instead of file path (optional)
            // This allows you to store the icon data without disk access
            //return await ConvertStreamToBase64DataUriAsync(memoryStream);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Grid icon creation error: {ex.Message}");
            return null;
        }
    }

    private static async Task<CanvasBitmap?> ToCanvasBitmapAsync(
        this ImageSource? imageSource,
        CanvasDevice device)
    {
        if (imageSource == null)
        {
            return null;
        }

        try
        {
            if (imageSource is BitmapImage bitmapImage)
            {
                if (bitmapImage.UriSource is not null)
                {
                    var streamReference = RandomAccessStreamReference.CreateFromUri(bitmapImage.UriSource);
                    using IRandomAccessStream stream = await streamReference.OpenReadAsync();

                    return await CanvasBitmap.LoadAsync(device, stream);
                }

                return null;
            }

            if (imageSource is WriteableBitmap writeableBitmap)
            {
                byte[] pixels = writeableBitmap.PixelBuffer.ToArray();
                var canvasBitmap = CanvasBitmap.CreateFromBytes(
                    device,
                    pixels,
                    writeableBitmap.PixelWidth,
                    writeableBitmap.PixelHeight,
                    Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized);

                return canvasBitmap;
            }

            throw new InvalidOperationException("Unsupported ImageSource type for conversion to CanvasBitmap.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error converting ImageSource to CanvasBitmap: {ex.Message}");
            return null;
        }
    }
}
