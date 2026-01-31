using CommunityToolkit.Mvvm.ComponentModel;
using ImageMagick;
using WindowSill.API;
using WindowSill.ImageHelper.Helpers;
using Windows.Storage;
using Path = System.IO.Path;

namespace WindowSill.ImageHelper.CompressImage;

internal sealed partial class CompressionTask : ObservableObject
{
    private readonly FileInfo _sourceFileInfo;
    private readonly string _outputPath;

    internal CompressionTask(IStorageFile storageFile)
    {
        _sourceFileInfo = new FileInfo(storageFile.Path);
        _outputPath = FilePathHelper.GetUniqueOutputPath(storageFile.Path, "_compressed");
        ByteLengthBeforeCompression = _sourceFileInfo.Length;

        IsRunning = true;
    }

    public string FileName => Path.GetFileName(_outputPath);

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
            // Copy the original file to the output path first
            File.Copy(_sourceFileInfo.FullName, _outputPath, overwrite: false);

            var outputFileInfo = new FileInfo(_outputPath);
            var optimizer = new ImageOptimizer
            {
                IgnoreUnsupportedFormats = true,
                OptimalCompression = true
            };
            optimizer.LosslessCompress(outputFileInfo);

            outputFileInfo.Refresh();

            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                ByteLengthAfterCompression = outputFileInfo.Length;
                int compressionPercentage = (int)Math.Round((1 - (double)ByteLengthAfterCompression / ByteLengthBeforeCompression) * 100, 2);

                CompressionPercentage = $"{compressionPercentage}%";
                IsRunning = false;
            });
        }
        catch (Exception ex)
        {
            // TODO: Log the exception and display it to the user.
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                IsRunning = false;
            });
        }
    }
}
