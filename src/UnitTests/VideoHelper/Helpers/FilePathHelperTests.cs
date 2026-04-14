using FluentAssertions;

using WindowSill.VideoHelper.Helpers;

using Path = System.IO.Path;

namespace UnitTests.VideoHelper.Helpers;

public class FilePathHelperTests
{
    [Fact]
    internal void GetUniqueOutputPath_AddsSuffix_WhenFileDoesNotExist()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            string original = Path.Combine(tempDir, "video.mp4");

            string result = FilePathHelper.GetUniqueOutputPath(original, "_compressed");

            result.Should().Be(Path.Combine(tempDir, "video_compressed.mp4"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    internal void GetUniqueOutputPath_AppendsCounter_WhenFileExists()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            string original = Path.Combine(tempDir, "video.mp4");
            // Create the file that would conflict
            File.WriteAllText(Path.Combine(tempDir, "video_compressed.mp4"), "exists");

            string result = FilePathHelper.GetUniqueOutputPath(original, "_compressed");

            result.Should().Be(Path.Combine(tempDir, "video_compressed_1.mp4"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    internal void GetUniqueOutputPath_IncrementsCounter_UntilUnique()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            string original = Path.Combine(tempDir, "video.mp4");
            File.WriteAllText(Path.Combine(tempDir, "video_compressed.mp4"), "exists");
            File.WriteAllText(Path.Combine(tempDir, "video_compressed_1.mp4"), "exists");
            File.WriteAllText(Path.Combine(tempDir, "video_compressed_2.mp4"), "exists");

            string result = FilePathHelper.GetUniqueOutputPath(original, "_compressed");

            result.Should().Be(Path.Combine(tempDir, "video_compressed_3.mp4"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    internal void GetUniqueOutputPath_ChangesExtension_WhenNewExtensionProvided()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            string original = Path.Combine(tempDir, "video.avi");

            string result = FilePathHelper.GetUniqueOutputPath(original, string.Empty, "mp4");

            result.Should().Be(Path.Combine(tempDir, "video.mp4"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    internal void GetUniqueOutputPath_KeepsOriginalExtension_WhenNewExtensionIsNull()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            string original = Path.Combine(tempDir, "video.mkv");

            string result = FilePathHelper.GetUniqueOutputPath(original, "_resized");

            result.Should().EndWith(".mkv");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
