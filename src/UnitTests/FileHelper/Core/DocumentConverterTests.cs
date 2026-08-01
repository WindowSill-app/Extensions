using System.Text;

using FluentAssertions;

using WindowSill.FileHelper.Core;

using Path = System.IO.Path;

namespace UnitTests.FileHelper.Core;

/// <summary>
/// Tests for <see cref="DocumentConverter"/>'s file handling: temp-directory isolation, the atomic non-overwriting
/// relocation of single-file output (including the Explorer-style "(n)" collision fallback), the multi-file
/// subfolder relocation (e.g. Markdown + images), progress reporting, cancellation, and cleanup on failure.
/// </summary>
/// <remarks>
/// The rendering engine is faked so these tests run headlessly and deterministically. The real Syncfusion rendering
/// (PDF and the Word-family formats) is verified end-to-end on the test VM rather than in this unit-test host.
/// </remarks>
public class DocumentConverterTests
{
    private static readonly byte[] s_fakeBytes = "%PDF-1.7\nfake body\n%%EOF"u8.ToArray();

    [Fact]
    internal void OutputExtension_ReflectsTheConfiguredExtension()
    {
        var converter = new DocumentConverter(new FakeRenderer(), ".md");
        converter.OutputExtension.Should().Be(".md");
    }

    [Fact]
    internal async Task ConvertAsync_ProducesTheOutputFileBesideSource_ForSingleFileOutput()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(tempDirectory, "source.docx");
            string outputPath = Path.Combine(tempDirectory, "source.pdf");
            File.WriteAllText(sourcePath, "ignored by the fake");

            var converter = new DocumentConverter(new FakeRenderer(), ".pdf");

            string? result = await converter.ConvertAsync(sourcePath, outputPath);

            result.Should().Be(outputPath);
            File.Exists(outputPath).Should().BeTrue();
            Encoding.ASCII.GetString(await File.ReadAllBytesAsync(outputPath), 0, 4).Should().Be("%PDF");

            // No leftover temp directory should remain beside the destination — only source + output.
            Directory.GetFileSystemEntries(tempDirectory).Should().BeEquivalentTo([sourcePath, outputPath]);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    internal async Task ConvertAsync_RelocatesIntoASelfContainedSubfolder_ForMultiFileOutput()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(tempDirectory, "source.docx");
            string outputPath = Path.Combine(tempDirectory, "source.md");
            File.WriteAllText(sourcePath, "ignored by the fake");

            // Simulate Markdown-with-images: the renderer writes the main file plus a sibling resource file.
            var converter = new DocumentConverter(new FakeRenderer(writeSidecarNamed: "source_images.txt"), ".md");

            string? result = await converter.ConvertAsync(sourcePath, outputPath);

            string expectedFolder = Path.Combine(tempDirectory, "source");
            string expectedMainFile = Path.Combine(expectedFolder, "source.md");
            result.Should().Be(expectedMainFile);
            File.Exists(expectedMainFile).Should().BeTrue();
            File.Exists(Path.Combine(expectedFolder, "source_images.txt")).Should().BeTrue();

            // The source folder holds only the original document and the new self-contained subfolder — no loose
            // resource files, and no leftover temp directory.
            Directory.GetFileSystemEntries(tempDirectory).Should().BeEquivalentTo([sourcePath, expectedFolder]);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    internal async Task ConvertAsync_ReportsMonotonicallyIncreasingProgressEndingAtOne()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(tempDirectory, "source.docx");
            string outputPath = Path.Combine(tempDirectory, "source.pdf");
            File.WriteAllText(sourcePath, "ignored by the fake");

            var converter = new DocumentConverter(new FakeRenderer(), ".pdf");

            var reportedValues = new List<double>();
            var progress = new SynchronousProgress<double>(reportedValues.Add);

            await converter.ConvertAsync(sourcePath, outputPath, progress);

            reportedValues.Should().NotBeEmpty();
            reportedValues.Should().BeInAscendingOrder();
            reportedValues[^1].Should().Be(1.0);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    internal async Task ConvertAsync_ThrowsAndLeavesNoOutput_WhenCancellationIsAlreadyRequested()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(tempDirectory, "source.docx");
            string outputPath = Path.Combine(tempDirectory, "source.pdf");
            File.WriteAllText(sourcePath, "ignored by the fake");

            var converter = new DocumentConverter(new FakeRenderer(), ".pdf");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<Task> act = async () => await converter.ConvertAsync(sourcePath, outputPath, cancellationToken: cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            Directory.GetFileSystemEntries(tempDirectory).Should().BeEquivalentTo([sourcePath]);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    internal async Task ConvertAsync_PropagatesFailureAndLeavesNoOutput_WhenRenderingThrows()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(tempDirectory, "source.docx");
            string outputPath = Path.Combine(tempDirectory, "source.pdf");
            File.WriteAllText(sourcePath, "ignored by the fake");

            var converter = new DocumentConverter(
                new FakeRenderer(throwWith: new InvalidOperationException("This document is corrupt.")), ".pdf");

            Func<Task> act = async () => await converter.ConvertAsync(sourcePath, outputPath);

            (await act.Should().ThrowAsync<InvalidOperationException>())
                .Which.Message.Should().Be("This document is corrupt.");
            Directory.GetFileSystemEntries(tempDirectory).Should().BeEquivalentTo([sourcePath]);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    internal async Task ConvertAsync_CleansUpTempDirectoryAndLeavesNoOutput_WhenRenderingWritesPartialThenThrows()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(tempDirectory, "source.docx");
            string outputPath = Path.Combine(tempDirectory, "source.pdf");
            File.WriteAllText(sourcePath, "ignored by the fake");

            var converter = new DocumentConverter(
                new FakeRenderer(writePartialThenThrow: new IOException("Rendering failed.")), ".pdf");

            Func<Task> act = async () => await converter.ConvertAsync(sourcePath, outputPath);

            await act.Should().ThrowAsync<IOException>();

            // The partially-written output (and the temp directory holding it) must have been cleaned up.
            Directory.GetFileSystemEntries(tempDirectory).Should().BeEquivalentTo([sourcePath]);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    internal async Task ConvertAsync_FallsBackToParenthesizedName_WhenSingleFileDestinationCollides()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(tempDirectory, "source.docx");
            string outputPath = Path.Combine(tempDirectory, "source.pdf");
            string fallbackPath = Path.Combine(tempDirectory, "source (1).pdf");
            File.WriteAllText(sourcePath, "ignored by the fake");

            // A file already occupies the requested destination; the converter must not overwrite it and must fall
            // back to the next "(n)" candidate.
            byte[] existingContent = "not a pdf, must not be overwritten"u8.ToArray();
            await File.WriteAllBytesAsync(outputPath, existingContent);

            var converter = new DocumentConverter(new FakeRenderer(), ".pdf");

            string? result = await converter.ConvertAsync(sourcePath, outputPath);

            result.Should().Be(fallbackPath);
            File.Exists(fallbackPath).Should().BeTrue();
            (await File.ReadAllBytesAsync(outputPath)).Should().BeEquivalentTo(existingContent);
            Directory.GetFileSystemEntries(tempDirectory).Should().BeEquivalentTo([sourcePath, outputPath, fallbackPath]);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    internal async Task ConvertAsync_FallsBackToParenthesizedSubfolder_WhenSubfolderCollides()
    {
        string tempDirectory = CreateTempDirectory();
        try
        {
            string sourcePath = Path.Combine(tempDirectory, "source.docx");
            string outputPath = Path.Combine(tempDirectory, "source.md");
            File.WriteAllText(sourcePath, "ignored by the fake");

            // A folder already occupies the natural destination folder name; the multi-file relocation must fall
            // back to the next "(n)" folder candidate.
            Directory.CreateDirectory(Path.Combine(tempDirectory, "source"));

            var converter = new DocumentConverter(new FakeRenderer(writeSidecarNamed: "source_images.txt"), ".md");

            string? result = await converter.ConvertAsync(sourcePath, outputPath);

            string expectedFolder = Path.Combine(tempDirectory, "source (1)");
            result.Should().Be(Path.Combine(expectedFolder, "source.md"));
            File.Exists(Path.Combine(expectedFolder, "source.md")).Should().BeTrue();
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"FileHelperTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        return path;
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
            // Best-effort cleanup only.
        }
    }

    /// <summary>
    /// A fake renderer that writes fake output bytes to the requested path, optionally writing a sibling resource
    /// file (to simulate multi-file output such as Markdown + images) or throwing (before or after writing a partial
    /// file) to exercise the converter's error/cleanup paths.
    /// </summary>
    private sealed class FakeRenderer(
        Exception? throwWith = null,
        Exception? writePartialThenThrow = null,
        string? writeSidecarNamed = null) : IDocumentRenderer
    {
        public void RenderToFile(string sourcePath, string outputFilePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (throwWith is not null)
            {
                throw throwWith;
            }

            if (writePartialThenThrow is not null)
            {
                File.WriteAllBytes(outputFilePath, "partial"u8.ToArray());
                throw writePartialThenThrow;
            }

            File.WriteAllBytes(outputFilePath, s_fakeBytes);

            if (writeSidecarNamed is not null)
            {
                string siblingPath = Path.Combine(Path.GetDirectoryName(outputFilePath)!, writeSidecarNamed);
                File.WriteAllBytes(siblingPath, "sidecar resource"u8.ToArray());
            }
        }
    }

    /// <summary>
    /// A synchronous <see cref="IProgress{T}"/> implementation that invokes the callback immediately on the
    /// reporting thread, unlike <see cref="Progress{T}"/> which marshals through a captured
    /// <see cref="SynchronizationContext"/> (or the thread pool) and would make ordering assertions flaky in tests.
    /// </summary>
    private sealed class SynchronousProgress<T>(Action<T> onReport) : IProgress<T>
    {
        public void Report(T value) => onReport(value);
    }
}
