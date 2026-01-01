using CommunityToolkit.Mvvm.ComponentModel;
using ImageMagick;
using WindowSill.API;
using Windows.Storage;

namespace WindowSill.ImageHelper.CompressImage;

internal sealed partial class CompressionTask : ObservableObject
{
    private readonly FileInfo _fileInfo;

    internal CompressionTask(IStorageFile storageFile)
    {
        _fileInfo = new FileInfo(storageFile.Path);
        ByteLengthBeforeCompression = _fileInfo.Length;

        IsRunning = true;
    }

    public string FileName => _fileInfo.Name;

    [ObservableProperty]
    public partial long ByteLengthBeforeCompression { get; set; }

    [ObservableProperty]
    public partial long ByteLengthAfterCompression { get; set; }

    [ObservableProperty]
    public partial string CompressionPercentage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    internal async Task LosslessCompressAsync()
    {
        try
        {
            var optimizer = new ImageOptimizer
            {
                IgnoreUnsupportedFormats = true,
                OptimalCompression = true
            };
            optimizer.LosslessCompress(_fileInfo);
        }
        catch (Exception ex)
        {
            // TODO: Log the exception and display it to the user.
        }

        _fileInfo.Refresh();

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ByteLengthAfterCompression = _fileInfo.Length;
            int compressionPercentage = (int)Math.Round((1 - (double)ByteLengthAfterCompression / ByteLengthBeforeCompression) * 100, 2);

            CompressionPercentage = $"{compressionPercentage}%";
            IsRunning = false;
        });
    }
}
