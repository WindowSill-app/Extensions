using System.ComponentModel.Composition;
using System.IO.Compression;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Default implementation of <see cref="IZipArchiveInspector"/>, reading the ZIP archive's central directory via
/// <see cref="ZipArchive"/> (opened in read-only mode, which never extracts entry content).
/// Exported as a MEF singleton so all components share the same instance.
/// </summary>
[Export(typeof(IZipArchiveInspector))]
internal sealed class ZipArchiveInspector : IZipArchiveInspector
{
    /// <inheritdoc />
    public Task<ZipArchiveInspectionResult> InspectAsync(string zipFilePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                using FileStream fileStream = File.OpenRead(zipFilePath);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false);

                int fileCount = 0;
                long compressedSize = 0;
                long uncompressedSize = 0;
                var entries = new List<ZipEntryInfo>(archive.Entries.Count);

                // Deduplicated set of folder paths, seeded both from explicit folder entries and from the
                // ancestor directory prefixes implied by every file entry's path (e.g. "a/b.txt" implies "a").
                // A folder present as both an explicit entry and an implied ancestor is counted only once.
                var folderPaths = new HashSet<string>(StringComparer.Ordinal);

                for (int i = 0; i < archive.Entries.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ZipArchiveEntry entry = archive.Entries[i];
                    string normalizedName = entry.FullName.Replace('\\', '/');

                    if (IsFolderEntry(entry, normalizedName))
                    {
                        AddFolderAndAncestors(folderPaths, normalizedName.TrimEnd('/'));
                    }
                    else
                    {
                        fileCount++;
                        compressedSize += entry.CompressedLength;
                        uncompressedSize += entry.Length;

                        // entry.Name is the file name only; FullName is the full relative path.
                        entries.Add(new ZipEntryInfo(entry.Name, normalizedName, entry.CompressedLength, entry.Length));

                        AddAncestorFolders(folderPaths, normalizedName);
                    }
                }

                var summary = new ZipArchiveSummary(fileCount, folderPaths.Count, compressedSize, uncompressedSize);
                return new ZipArchiveInspectionResult(summary, entries);
            },
            cancellationToken);
    }

    /// <summary>
    /// Determines whether a ZIP entry represents an explicit folder entry, i.e. its (normalized) name ends with a
    /// '/' path separator and it has no content. Not every ZIP writer emits explicit folder entries: folders
    /// implied only by nested file paths (e.g. "a/b.txt" without an "a/" entry) are handled separately by
    /// <see cref="AddAncestorFolders"/>.
    /// </summary>
    private static bool IsFolderEntry(ZipArchiveEntry entry, string normalizedName)
        => entry.Length == 0 && normalizedName.EndsWith('/');

    /// <summary>
    /// Adds every ancestor directory prefix of <paramref name="filePath"/> (a normalized, '/'-separated file path)
    /// to <paramref name="folderPaths"/>. For example, "a/b/c.txt" adds "a" and "a/b".
    /// </summary>
    private static void AddAncestorFolders(HashSet<string> folderPaths, string filePath)
    {
        int index = filePath.IndexOf('/');
        while (index >= 0)
        {
            folderPaths.Add(filePath[..index]);
            index = filePath.IndexOf('/', index + 1);
        }
    }

    /// <summary>
    /// Adds <paramref name="folderPath"/> (a normalized, '/'-separated folder path with no trailing separator)
    /// and all of its own ancestor directory prefixes to <paramref name="folderPaths"/>, so an explicit folder
    /// entry such as "a/b/" is counted along with its implied parent "a", even if "a/" is not itself an explicit
    /// entry in the archive.
    /// </summary>
    private static void AddFolderAndAncestors(HashSet<string> folderPaths, string folderPath)
    {
        if (folderPath.Length == 0)
        {
            return;
        }

        folderPaths.Add(folderPath);
        AddAncestorFolders(folderPaths, folderPath);
    }
}
