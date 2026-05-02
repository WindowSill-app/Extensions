using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.ClipboardHistory.Core;

/// <summary>
/// Writes clipboard content (text or image) as a file in the specified folder.
/// </summary>
internal static class ClipboardFileWriter
{
    private static readonly ILogger logger = typeof(ClipboardFileWriter).Log();

    /// <summary>
    /// Attempts to write the clipboard data as a file in <paramref name="folderPath"/>.
    /// Returns <c>true</c> if a file was created; <c>false</c> if the data type is unsupported.
    /// </summary>
    /// <param name="data">The clipboard data package.</param>
    /// <param name="dataType">The detected data type of the clipboard item.</param>
    /// <param name="folderPath">The folder in which to create the file.</param>
    internal static async Task<bool> TryWriteAsFileAsync(DataPackageView data, DetectedClipboardDataType dataType, string folderPath)
    {
        string baseName = "/WindowSill.ClipboardHistory/Misc/ClipboardFileBaseName".GetLocalizedString();
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "Clipboard";
        }

        try
        {
            return dataType switch
            {
                DetectedClipboardDataType.Text
                or DetectedClipboardDataType.Rtf
                or DetectedClipboardDataType.Uri
                or DetectedClipboardDataType.Color
                or DetectedClipboardDataType.Html
                => await WriteTextAsync(data, folderPath, baseName),

                DetectedClipboardDataType.Image => await WriteImageAsync(data, folderPath, baseName),
                _ => false,
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write clipboard content as file in {Folder}.", folderPath);
            return false;
        }
    }

    private static async Task<bool> WriteTextAsync(DataPackageView data, string folderPath, string baseName)
    {
        string? text = null;

        if (data.AvailableFormats.Contains(StandardDataFormats.Text))
        {
            text = await data.GetTextAsync();
        }

        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        string filePath = GetUniquePath(folderPath, baseName, ".txt");
        await File.WriteAllTextAsync(filePath, text, System.Text.Encoding.UTF8);
        NotifyShell(filePath, folderPath);

        logger.LogInformation("Clipboard text saved to {FilePath}.", filePath);
        return true;
    }

    private static async Task<bool> WriteImageAsync(DataPackageView data, string folderPath, string baseName)
    {
        if (!data.AvailableFormats.Contains(StandardDataFormats.Bitmap))
        {
            return false;
        }

        RandomAccessStreamReference streamRef = await data.GetBitmapAsync();
        using IRandomAccessStreamWithContentType stream = await streamRef.OpenReadAsync();

        string filePath = GetUniquePath(folderPath, baseName, ".png");

        using (FileStream fileStream = File.Create(filePath))
        {
            using Stream inputStream = stream.AsStreamForRead();
            await inputStream.CopyToAsync(fileStream);
        }

        NotifyShell(filePath, folderPath);

        logger.LogInformation("Clipboard image saved to {FilePath}.", filePath);
        return true;
    }

    /// <summary>
    /// Returns a unique file path in the form "baseName.ext", "baseName (1).ext", etc.
    /// </summary>
    private static string GetUniquePath(string folder, string baseName, string extension)
    {
        string path = Path.Combine(folder, baseName + extension);
        if (!File.Exists(path))
        {
            return path;
        }

        for (int n = 1; n < 100_000; n++)
        {
            path = Path.Combine(folder, $"{baseName} ({n}){extension}");
            if (!File.Exists(path))
            {
                return path;
            }
        }

        // Fallback — extremely unlikely
        return Path.Combine(folder, baseName + extension);
    }

    /// <summary>
    /// Notifies the Shell that a file was created so Explorer refreshes.
    /// </summary>
    private static unsafe void NotifyShell(string filePath, string folderPath)
    {
        fixed (char* pFile = filePath)
        fixed (char* pFolder = folderPath)
        {
            PInvoke.SHChangeNotify(
                SHCNE_ID.SHCNE_CREATE,
                SHCNF_FLAGS.SHCNF_PATH | SHCNF_FLAGS.SHCNF_FLUSH,
                pFile,
                null);

            PInvoke.SHChangeNotify(
                SHCNE_ID.SHCNE_UPDATEDIR,
                SHCNF_FLAGS.SHCNF_PATH | SHCNF_FLAGS.SHCNF_FLUSH,
                pFolder,
                null);
        }
    }
}
