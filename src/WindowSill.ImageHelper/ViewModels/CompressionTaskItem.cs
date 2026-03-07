using CommunityToolkit.Mvvm.ComponentModel;

using WindowSill.API;
using WindowSill.ImageHelper.Core;
using WindowSill.ImageHelper.Helpers;

using Windows.Storage;

using Path = System.IO.Path;

namespace WindowSill.ImageHelper.ViewModels;

/// <summary>
/// Represents a single image compression task with observable progress state.
/// </summary>
internal sealed partial class CompressionTaskItem : ObservableObject
{
    private readonly FileInfo _sourceFileInfo;
    private readonly string _outputPath;
    private readonly IImageCompressor _compressor;

    internal CompressionTaskItem(IStorageFile storageFile, IImageCompressor compressor)
    {
        _sourceFileInfo = new FileInfo(storageFile.Path);
        _outputPath = FilePathHelper.GetUniqueOutputPath(storageFile.Path, "_compressed");
        _compressor = compressor;

        ByteLengthBeforeCompression = _sourceFileInfo.Length;
        IsRunning = true;
    }

    /// <summary>
    /// Gets the output file name.
    /// </summary>
    public string FileName => Path.GetFileName(_outputPath);

    /// <summary>
    /// Gets or sets the file size before compression.
    /// </summary>
    [ObservableProperty]
    public partial long ByteLengthBeforeCompression { get; set; }

    /// <summary>
    /// Gets or sets the file size after compression.
    /// </summary>
    [ObservableProperty]
    public partial long ByteLengthAfterCompression { get; set; }

    /// <summary>
    /// Gets or sets the compression percentage as a display string.
    /// </summary>
    [ObservableProperty]
    public partial string CompressionPercentage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the compression is currently running.
    /// </summary>
    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    /// <summary>
    /// Runs lossless compression on the file.
    /// </summary>
    internal async Task CompressAsync()
    {
        try
        {
            long compressedSize = await Task.Run(() => _compressor.LosslessCompress(_sourceFileInfo.FullName, _outputPath));

            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                ByteLengthAfterCompression = compressedSize;
                int percentage = (int)Math.Round((1 - (double)ByteLengthAfterCompression / ByteLengthBeforeCompression) * 100, 2);
                CompressionPercentage = $"{percentage}%";
                IsRunning = false;
            });
        }
        catch (Exception)
        {
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                IsRunning = false;
            });
        }
    }
}
