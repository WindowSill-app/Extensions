using CommunityToolkit.Mvvm.ComponentModel;
using ImageMagick;
using WindowSill.API;
using Windows.Storage;
using Path = System.IO.Path;

namespace WindowSill.ImageHelper.ConvertImage;

internal sealed partial class ConversionTask : ObservableObject
{
    private readonly FileInfo _fileInfo;
    private readonly MagickFormat _format;

    internal ConversionTask(IStorageFile storageFile, MagickFormat format)
    {
        _fileInfo = new FileInfo(storageFile.Path);
        _format = format;

        IsRunning = true;
    }

    public string FileName => _fileInfo.Name;

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial bool IsSucceeded { get; set; }

    [ObservableProperty]
    public partial bool IsFailed { get; set; }

    internal async Task ConvertAsync(CancellationToken cancellationToken)
    {
        bool isSucceeded = false;
        try
        {
            string newFileName = Path.GetFileNameWithoutExtension(_fileInfo.FullName) + "." + _format.ToString().ToLowerInvariant();
            string newFilePath = Path.Combine(_fileInfo.DirectoryName!, newFileName);

            if (_fileInfo.Exists && !File.Exists(newFilePath))
            {
                using var originalImage = new MagickImage(_fileInfo);
                originalImage.Format = _format;
                byte[] data = originalImage.ToByteArray();
                await File.WriteAllBytesAsync(newFilePath, data, cancellationToken);
            }

            isSucceeded = true;
        }
        catch (Exception ex)
        {
            // TODO: Log the exception and display it to the user.
        }

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            IsRunning = false;
            IsSucceeded = isSucceeded;
            IsFailed = !isSucceeded;
        });
    }
}

