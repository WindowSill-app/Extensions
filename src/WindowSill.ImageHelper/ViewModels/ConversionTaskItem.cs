using CommunityToolkit.Mvvm.ComponentModel;

using ImageMagick;

using WindowSill.API;
using WindowSill.ImageHelper.Core;
using WindowSill.ImageHelper.Helpers;

using Windows.Storage;

namespace WindowSill.ImageHelper.ViewModels;

/// <summary>
/// Represents a single image conversion task with observable progress state.
/// </summary>
internal sealed partial class ConversionTaskItem : ObservableObject
{
    private readonly FileInfo _fileInfo;
    private readonly MagickFormat _format;
    private readonly IImageConverter _converter;

    internal ConversionTaskItem(IStorageFile storageFile, MagickFormat format, IImageConverter converter)
    {
        _fileInfo = new FileInfo(storageFile.Path);
        _format = format;
        _converter = converter;

        IsRunning = true;
    }

    /// <summary>
    /// Gets the source file name.
    /// </summary>
    public string FileName => _fileInfo.Name;

    /// <summary>
    /// Gets or sets whether the conversion is currently running.
    /// </summary>
    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    /// <summary>
    /// Gets or sets whether the conversion succeeded.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSucceeded { get; set; }

    /// <summary>
    /// Gets or sets whether the conversion failed.
    /// </summary>
    [ObservableProperty]
    public partial bool IsFailed { get; set; }

    /// <summary>
    /// Runs the image format conversion.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    internal async Task ConvertAsync(CancellationToken cancellationToken)
    {
        bool isSucceeded = false;
        try
        {
            string newExtension = _format.ToString().ToLowerInvariant();
            string newFilePath = FilePathHelper.GetUniqueOutputPath(_fileInfo.FullName, "_converted", newExtension);

            if (_fileInfo.Exists)
            {
                await _converter.ConvertAsync(_fileInfo.FullName, newFilePath, _format, cancellationToken);
            }

            isSucceeded = true;
        }
        catch (Exception)
        {
            // Conversion failed for this file.
        }

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            IsRunning = false;
            IsSucceeded = isSucceeded;
            IsFailed = !isSucceeded;
        });
    }
}
