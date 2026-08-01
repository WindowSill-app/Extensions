using WindowSill.FileHelper.Helpers;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Runs a file-producing operation into an isolated temporary directory and only then moves the result into place,
/// so a cancelled or failed operation never leaves a partial, corrupt, or leftover file behind, and an existing
/// file is never overwritten.
/// </summary>
/// <remarks>
/// <para>
/// The temporary directory is created beside the final destination (same volume), so the eventual move is an atomic
/// rename rather than a cross-volume copy. Once the operation succeeds:
/// </para>
/// <list type="bullet">
/// <item>If it produced a single file, that file is moved next to the source via an atomic, non-overwriting move,
/// falling back to the next Explorer-style "(n)" candidate name on a late collision.</item>
/// <item>If it produced multiple files (e.g. Markdown plus an images folder, or a PDF split into one file per
/// page), the whole set is relocated into a dedicated, collision-safe subfolder, keeping the output self-contained
/// and the destination folder free of loose files.</item>
/// </list>
/// </remarks>
internal static class SafeOutputWriter
{
    /// <summary>
    /// Upper bound on collision-retry attempts when moving the output into place, to avoid an unbounded loop in the
    /// pathological case where the destination directory is being flooded with same-named files concurrently.
    /// </summary>
    private const int MaxMoveAttempts = 1000;

    /// <summary>
    /// Runs <paramref name="write"/> against an isolated temporary path and relocates whatever it produced next to
    /// the requested destination.
    /// </summary>
    /// <param name="directory">The directory the final output should end up in.</param>
    /// <param name="fileNameWithoutExtension">The desired output file name, without extension.</param>
    /// <param name="extension">The desired output extension, including the leading dot (e.g. <c>.pdf</c>).</param>
    /// <param name="write">
    /// Writes the output to the temporary path it is given. It may also emit sibling files/folders next to that
    /// path; the whole set is then relocated together.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The actual path the primary output ended up at.</returns>
    internal static string WriteAndRelocate(
        string directory,
        string fileNameWithoutExtension,
        string extension,
        Action<string> write,
        CancellationToken cancellationToken)
        => WriteAndRelocateCore(
            directory,
            fileNameWithoutExtension,
            extension,
            tempOutputFile =>
            {
                write(tempOutputFile);
                return Task.CompletedTask;
            },
            cancellationToken)
            .GetAwaiter()
            .GetResult();

    /// <summary>
    /// Asynchronous counterpart of <see cref="WriteAndRelocate"/>, for producers that are inherently asynchronous
    /// (such as the Windows PDF rasterizer) and so cannot be wrapped in a synchronous callback.
    /// </summary>
    /// <param name="directory">The directory the final output should end up in.</param>
    /// <param name="fileNameWithoutExtension">The desired output file name, without extension.</param>
    /// <param name="extension">The desired output extension, including the leading dot.</param>
    /// <param name="writeAsync">Writes the output to the temporary path it is given.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The actual path the primary output ended up at.</returns>
    internal static Task<string> WriteAndRelocateAsync(
        string directory,
        string fileNameWithoutExtension,
        string extension,
        Func<string, Task> writeAsync,
        CancellationToken cancellationToken)
        => WriteAndRelocateCore(directory, fileNameWithoutExtension, extension, writeAsync, cancellationToken);

    private static async Task<string> WriteAndRelocateCore(
        string directory,
        string fileNameWithoutExtension,
        string extension,
        Func<string, Task> writeAsync,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Isolated, unpredictable temp directory beside the destination. Writing into a directory (rather than a
        // single temp file) lets us discover any sibling files the operation emits and relocate them as a set.
        string tempDirectory = Path.Combine(directory, $".{fileNameWithoutExtension}.{Guid.NewGuid():N}.tmpdir");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // Use the real base name inside the temp directory so any derived sibling folder (e.g. Markdown's
            // "<name>_images") is named naturally and its relative references remain valid after relocation.
            string tempOutputFile = Path.Combine(tempDirectory, fileNameWithoutExtension + extension);
            await writeAsync(tempOutputFile).ConfigureAwait(false);

            // A cancellation that arrived while the output was being written must still leave nothing behind.
            cancellationToken.ThrowIfCancellationRequested();

            string[] producedEntries = Directory.GetFileSystemEntries(tempDirectory);
            bool singleFileOutput = producedEntries.Length == 1 && File.Exists(tempOutputFile);

            return singleFileOutput
                ? MoveFileToUniqueDestination(tempOutputFile, directory, fileNameWithoutExtension, extension)
                : MoveDirectoryContentsToUniqueSubfolder(tempDirectory, directory, fileNameWithoutExtension, extension);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    /// <summary>
    /// Atomically moves <paramref name="tempFilePath"/> to the requested destination
    /// (<paramref name="fileNameWithoutExtension"/> + <paramref name="extension"/> in <paramref name="directory"/>),
    /// never overwriting an existing file, retrying against the next Explorer-style "(n)" candidate on a late
    /// collision.
    /// </summary>
    /// <returns>The actual destination path the file was moved to.</returns>
    private static string MoveFileToUniqueDestination(string tempFilePath, string directory, string fileNameWithoutExtension, string extension)
    {
        for (int counter = 0; counter < MaxMoveAttempts; counter++)
        {
            string candidatePath = OutputPathHelper.GetCandidatePath(directory, fileNameWithoutExtension, extension, counter);
            try
            {
                File.Move(tempFilePath, candidatePath, overwrite: false);
                return candidatePath;
            }
            catch (IOException) when (File.Exists(candidatePath))
            {
                // Destination was created concurrently since the caller's pre-check; retry with the next
                // "(n)" candidate instead of overwriting it or giving up.
            }
        }

        throw new IOException(
            $"Could not find a unique destination file name for '{fileNameWithoutExtension}{extension}' in '{directory}' after {MaxMoveAttempts} attempts.");
    }

    /// <summary>
    /// Relocates a multi-file result into a dedicated, collision-safe subfolder beside the destination (named after
    /// the output, with an Explorer-style "(n)" suffix on collision), keeping the output self-contained.
    /// </summary>
    /// <returns>The path of the primary output file inside the new subfolder.</returns>
    private static string MoveDirectoryContentsToUniqueSubfolder(string tempDirectory, string directory, string fileNameWithoutExtension, string extension)
    {
        for (int counter = 0; counter < MaxMoveAttempts; counter++)
        {
            // Reuse the shared "(n)" naming with an empty extension so the candidate is a folder name.
            string candidateFolder = OutputPathHelper.GetCandidatePath(directory, fileNameWithoutExtension, extensionWithDot: string.Empty, counter);
            if (Directory.Exists(candidateFolder) || File.Exists(candidateFolder))
            {
                continue;
            }

            try
            {
                Directory.Move(tempDirectory, candidateFolder);
                return Path.Combine(candidateFolder, fileNameWithoutExtension + extension);
            }
            catch (IOException) when (Directory.Exists(candidateFolder) || File.Exists(candidateFolder))
            {
                // The folder was created concurrently since the existence check above; retry with the next
                // "(n)" candidate.
            }
        }

        throw new IOException(
            $"Could not find a unique destination folder name for '{fileNameWithoutExtension}' in '{directory}' after {MaxMoveAttempts} attempts.");
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup only; nothing more we can do if the temp directory is locked or already gone
            // (e.g. it was renamed into place by a successful multi-file relocation).
        }
    }
}
