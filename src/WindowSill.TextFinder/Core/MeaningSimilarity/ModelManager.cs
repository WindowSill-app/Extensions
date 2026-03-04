using Path = System.IO.Path;

namespace WindowSill.TextFinder.Core.MeaningSimilarity;

/// <summary>
/// Downloads the all-MiniLM-L6-v2 sentence transformer model from Hugging Face.
/// </summary>
/// <remarks>
/// The model is cached locally in the plugin's data folder to avoid repeated downloads.
/// Model is licensed under Apache 2.0: https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2
/// </remarks>
internal static class ModelManager
{
    private const string ModelName = "all-MiniLM-L6-v2";
    private const string ModelUrl = $"https://huggingface.co/sentence-transformers/{ModelName}/resolve/main/onnx/model.onnx";
    private const string VocabUrl = $"https://huggingface.co/sentence-transformers/{ModelName}/resolve/main/vocab.txt";
    internal const string ModelFileName = "model.onnx";
    internal const string VocabFileName = "vocab.txt";

    /// <summary>
    /// Checks whether the model files exist in the specified directory.
    /// </summary>
    /// <param name="modelDirectory">Directory where model files should be stored.</param>
    /// <returns>True if both model.onnx and vocab.txt exist; otherwise false.</returns>
    public static bool ModelExists(string modelDirectory)
    {
        string modelPath = Path.Combine(modelDirectory, ModelFileName);
        string vocabPath = Path.Combine(modelDirectory, VocabFileName);
        return File.Exists(modelPath) && File.Exists(vocabPath);
    }

    /// <summary>
    /// Gets the expected model directory path within the plugin data folder.
    /// </summary>
    /// <param name="pluginDataFolder">The plugin's data folder path.</param>
    /// <returns>The path to the model subdirectory.</returns>
    public static string GetModelDirectory(string pluginDataFolder)
        => Path.Combine(pluginDataFolder, ModelName);

    /// <summary>
    /// Downloads model.onnx and vocab.txt if they don't already exist locally.
    /// </summary>
    /// <param name="modelDirectory">Directory to store the model files.</param>
    /// <param name="progress">Optional progress reporter (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Token to cancel the download operation.</param>
    /// <remarks>
    /// If the download is cancelled or fails, any partially downloaded files are deleted.
    /// </remarks>
    public static async Task DownloadModelAsync(
        string modelDirectory,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(modelDirectory);

        string modelPath = Path.Combine(modelDirectory, ModelFileName);
        string vocabPath = Path.Combine(modelDirectory, VocabFileName);

        if (!File.Exists(modelPath))
        {
            await DownloadFileWithCleanupAsync(ModelUrl, modelPath, progress, cancellationToken);
        }

        if (!File.Exists(vocabPath))
        {
            // Vocab file is small, no need to report progress
            await DownloadFileWithCleanupAsync(VocabUrl, vocabPath, progress: null, cancellationToken);
        }
    }

    /// <summary>
    /// Deletes any partially downloaded model files from the specified directory.
    /// </summary>
    /// <param name="modelDirectory">Directory containing the model files.</param>
    public static void CleanupPartialDownload(string modelDirectory)
    {
        TryDeleteFile(Path.Combine(modelDirectory, ModelFileName));
        TryDeleteFile(Path.Combine(modelDirectory, VocabFileName));
    }

    private static async Task DownloadFileWithCleanupAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            await DownloadFileAsync(url, destinationPath, progress, cancellationToken);
        }
        catch
        {
            // Clean up partial file on any failure
            TryDeleteFile(destinationPath);
            throw;
        }
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (IOException)
        {
            // Ignore deletion failures - file may be locked
        }
    }

    private static async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        byte[] buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (totalBytes.HasValue)
            {
                progress?.Report((double)totalRead / totalBytes.Value);
            }
        }
    }
}
