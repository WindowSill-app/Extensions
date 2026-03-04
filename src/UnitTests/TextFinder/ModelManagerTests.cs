using FluentAssertions;
using WindowSill.TextFinder.Core.MeaningSimilarity;
using Path = System.IO.Path;

namespace UnitTests.TextFinder;

/// <summary>
/// Tests for <see cref="ModelManager"/> covering model existence checks,
/// directory path construction, and partial download cleanup.
/// </summary>
public class ModelManagerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"ModelManagerTests-{Guid.NewGuid()}");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region ModelExists

    [Fact]
    public void ModelExists_BothFilesPresent_ReturnsTrue()
    {
        // Arrange
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, ModelManager.ModelFileName), string.Empty);
        File.WriteAllText(Path.Combine(_tempDir, ModelManager.VocabFileName), string.Empty);

        // Act
        bool result = ModelManager.ModelExists(_tempDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ModelExists_OnlyModelFile_ReturnsFalse()
    {
        // Arrange
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, ModelManager.ModelFileName), string.Empty);

        // Act
        bool result = ModelManager.ModelExists(_tempDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ModelExists_OnlyVocabFile_ReturnsFalse()
    {
        // Arrange
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, ModelManager.VocabFileName), string.Empty);

        // Act
        bool result = ModelManager.ModelExists(_tempDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ModelExists_NoFiles_ReturnsFalse()
    {
        // Arrange
        Directory.CreateDirectory(_tempDir);

        // Act
        bool result = ModelManager.ModelExists(_tempDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ModelExists_NonexistentDirectory_ReturnsFalse()
    {
        // Act
        bool result = ModelManager.ModelExists(_tempDir);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetModelDirectory

    [Fact]
    public void GetModelDirectory_ReturnsCombinedPath()
    {
        // Arrange
        string pluginDataFolder = @"C:\Users\Test\AppData";

        // Act
        string result = ModelManager.GetModelDirectory(pluginDataFolder);

        // Assert
        result.Should().Be(Path.Combine(pluginDataFolder, "all-MiniLM-L6-v2"));
    }

    #endregion

    #region CleanupPartialDownload

    [Fact]
    public void CleanupPartialDownload_DeletesExistingFiles()
    {
        // Arrange
        Directory.CreateDirectory(_tempDir);
        string modelPath = Path.Combine(_tempDir, ModelManager.ModelFileName);
        string vocabPath = Path.Combine(_tempDir, ModelManager.VocabFileName);
        File.WriteAllText(modelPath, "data");
        File.WriteAllText(vocabPath, "data");

        // Act
        ModelManager.CleanupPartialDownload(_tempDir);

        // Assert
        File.Exists(modelPath).Should().BeFalse();
        File.Exists(vocabPath).Should().BeFalse();
    }

    [Fact]
    public void CleanupPartialDownload_MissingFiles_DoesNotThrow()
    {
        // Arrange
        Directory.CreateDirectory(_tempDir);

        // Act
        Action act = () => ModelManager.CleanupPartialDownload(_tempDir);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void CleanupPartialDownload_NonexistentDirectory_DoesNotThrow()
    {
        // Act
        Action act = () => ModelManager.CleanupPartialDownload(_tempDir);

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}
