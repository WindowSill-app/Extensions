using FluentAssertions;

using WindowSill.VideoHelper.Core;

using Path = System.IO.Path;

namespace UnitTests.VideoHelper.Core;

public class FFmpegManagerTests
{
    [Fact]
    internal void GetFFmpegPath_CombinesDirectoryAndExecutable()
    {
        string result = FFmpegManager.GetFFmpegPath(@"C:\data\ffmpeg");

        result.Should().Be(@"C:\data\ffmpeg\ffmpeg.exe");
    }

    [Fact]
    internal void GetFFprobePath_CombinesDirectoryAndExecutable()
    {
        string result = FFmpegManager.GetFFprobePath(@"C:\data\ffmpeg");

        result.Should().Be(@"C:\data\ffmpeg\ffprobe.exe");
    }

    [Fact]
    internal void GetFFmpegDirectory_CombinesPluginFolderAndSubfolder()
    {
        string result = FFmpegManager.GetFFmpegDirectory(@"C:\users\me\AppData\plugin");

        result.Should().Be(@"C:\users\me\AppData\plugin\ffmpeg");
    }

    [Fact]
    internal void FFmpegExists_ReturnsFalse_WhenDirectoryDoesNotExist()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        FFmpegManager.FFmpegExists(nonExistent).Should().BeFalse();
    }

    [Fact]
    internal void FFmpegExists_ReturnsFalse_WhenOnlyOneFileExists()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "ffmpeg.exe"), "fake");

            FFmpegManager.FFmpegExists(tempDir).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    internal void FFmpegExists_ReturnsTrue_WhenBothFilesExist()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "ffmpeg.exe"), "fake");
            File.WriteAllText(Path.Combine(tempDir, "ffprobe.exe"), "fake");

            FFmpegManager.FFmpegExists(tempDir).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
