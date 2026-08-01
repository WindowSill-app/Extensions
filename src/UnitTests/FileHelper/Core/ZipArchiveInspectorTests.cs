using System.IO.Compression;
using System.Linq;

using FluentAssertions;

using WindowSill.FileHelper.Core;

using Path = System.IO.Path;

namespace UnitTests.FileHelper.Core;

public class ZipArchiveInspectorTests
{
    private readonly ZipArchiveInspector _inspector = new();

    [Fact]
    internal async Task InspectAsync_CountsFilesAndFoldersAndSizes_ForTypicalArchive()
    {
        string zipPath = CreateTempZipPath();

        try
        {
            using (var fileStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                // Explicit folder entry.
                archive.CreateEntry("folder/");

                ZipArchiveEntry entry1 = archive.CreateEntry("readme.txt", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(entry1.Open()))
                {
                    writer.Write(new string('a', 1000));
                }

                ZipArchiveEntry entry2 = archive.CreateEntry("folder/nested.txt", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(entry2.Open()))
                {
                    writer.Write(new string('b', 500));
                }
            }

            ZipArchiveInspectionResult result = await _inspector.InspectAsync(zipPath);
            ZipArchiveSummary summary = result.Summary;

            summary.FileCount.Should().Be(2);
            summary.FolderCount.Should().Be(1);
            summary.UncompressedSizeInBytes.Should().Be(1500);
            summary.CompressedSizeInBytes.Should().BeGreaterThan(0);
            summary.CompressedSizeInBytes.Should().BeLessThanOrEqualTo(summary.UncompressedSizeInBytes);
            summary.HasMeaningfulCompressionRatio.Should().BeTrue();
            summary.IsEmpty.Should().BeFalse();

            // The result also exposes the individual file entries (folders excluded) with their names and sizes.
            result.Entries.Should().HaveCount(2);
            result.Entries.Select(entry => entry.RelativePath)
                .Should().BeEquivalentTo(["readme.txt", "folder/nested.txt"]);
            ZipEntryInfo readme = result.Entries.Single(entry => entry.Name == "readme.txt");
            readme.UncompressedSizeInBytes.Should().Be(1000);
            readme.CompressedSizeInBytes.Should().BeGreaterThan(0);
        }
        finally
        {
            TryDelete(zipPath);
        }
    }

    [Fact]
    internal async Task InspectAsync_ReturnsEmptySummary_ForEmptyArchive()
    {
        string zipPath = CreateTempZipPath();

        try
        {
            using (var fileStream = new FileStream(zipPath, FileMode.Create))
            using (_ = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                // No entries added at all.
            }

            ZipArchiveInspectionResult result = await _inspector.InspectAsync(zipPath);
            ZipArchiveSummary summary = result.Summary;

            summary.FileCount.Should().Be(0);
            summary.FolderCount.Should().Be(0);
            summary.CompressedSizeInBytes.Should().Be(0);
            summary.UncompressedSizeInBytes.Should().Be(0);
            summary.IsEmpty.Should().BeTrue();
            summary.HasMeaningfulCompressionRatio.Should().BeFalse();
            result.Entries.Should().BeEmpty();
        }
        finally
        {
            TryDelete(zipPath);
        }
    }

    [Fact]
    internal async Task InspectAsync_CountsZeroByteFileEntry_AsFileNotFolder()
    {
        string zipPath = CreateTempZipPath();

        try
        {
            using (var fileStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                // A zero-byte file entry (no trailing slash) is still a file, not a folder.
                archive.CreateEntry("empty.txt");
            }

            ZipArchiveInspectionResult result = await _inspector.InspectAsync(zipPath);
            ZipArchiveSummary summary = result.Summary;

            summary.FileCount.Should().Be(1);
            summary.FolderCount.Should().Be(0);
            summary.HasMeaningfulCompressionRatio.Should().BeFalse();
            result.Entries.Should().ContainSingle(entry => entry.RelativePath == "empty.txt");
        }
        finally
        {
            TryDelete(zipPath);
        }
    }

    [Fact]
    internal async Task InspectAsync_CountsImplicitFolderPrefixes_WhenNoExplicitFolderEntryExists()
    {
        string zipPath = CreateTempZipPath();

        try
        {
            using (var fileStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                // No explicit "docs/" or "docs/nested/" folder entries at all — some zip writers (e.g. many
                // command-line tools) never emit them, only the file entries with slash-separated paths.
                archive.CreateEntry("docs/nested/report.txt");
                archive.CreateEntry("docs/summary.txt");
            }

            ZipArchiveInspectionResult result = await _inspector.InspectAsync(zipPath);
            ZipArchiveSummary summary = result.Summary;

            summary.FileCount.Should().Be(2);
            // "docs" and "docs/nested" are both implied by the file paths above, and must be counted even
            // though neither has an explicit folder (".../") entry in the archive.
            summary.FolderCount.Should().Be(2);
        }
        finally
        {
            TryDelete(zipPath);
        }
    }

    [Fact]
    internal async Task InspectAsync_DeduplicatesFolderCount_WhenExplicitAndImplicitFolderEntriesOverlap()
    {
        string zipPath = CreateTempZipPath();

        try
        {
            using (var fileStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                // An explicit "folder/" entry AND a file that implies the very same folder must only count once.
                archive.CreateEntry("folder/");
                archive.CreateEntry("folder/nested.txt");
                archive.CreateEntry("folder/again.txt");
            }

            ZipArchiveInspectionResult result = await _inspector.InspectAsync(zipPath);
            ZipArchiveSummary summary = result.Summary;

            summary.FileCount.Should().Be(2);
            summary.FolderCount.Should().Be(1);
        }
        finally
        {
            TryDelete(zipPath);
        }
    }

    [Fact]
    internal async Task InspectAsync_Throws_ForCorruptOrNonZipFile()
    {
        string filePath = CreateTempZipPath();

        try
        {
            File.WriteAllText(filePath, "This is definitely not a ZIP archive.");

            Func<Task> act = async () => await _inspector.InspectAsync(filePath);

            await act.Should().ThrowAsync<InvalidDataException>();
        }
        finally
        {
            TryDelete(filePath);
        }
    }

    private static string CreateTempZipPath()
        => Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
