using FluentAssertions;
using WindowSill.TextFinder.Core;

namespace UnitTests.TextFinder;

/// <summary>
/// Unit tests for <see cref="TextSearchHelper.CosineSimilarity"/> verifying
/// cosine similarity computation across identical, orthogonal, opposite,
/// similar, scaled, and single-element vector pairs.
/// </summary>
public class CosineSimilarityTests
{
    [Theory]
    [InlineData(new float[] { 1, 0, 0 }, new float[] { 1, 0, 0 }, 1.0f)]
    [InlineData(new float[] { 1, 0, 0 }, new float[] { 0, 1, 0 }, 0.0f)]
    [InlineData(new float[] { 1, 0 }, new float[] { -1, 0 }, -1.0f)]
    [InlineData(new float[] { 1, 0 }, new float[] { 5, 0 }, 1.0f)]
    [InlineData(new float[] { 3 }, new float[] { 4 }, 1.0f)]
    public void CosineSimilarity_WithKnownVectors_ReturnsExpectedValue(float[] a, float[] b, float expected)
    {
        // Act
        float result = TextSearchHelper.CosineSimilarity(a, b);

        // Assert
        result.Should().BeApproximately(expected, 0.001f);
    }

    [Fact]
    public void CosineSimilarity_WithSimilarVectors_ReturnsCloseToOne()
    {
        // Arrange
        float[] a = [1, 2, 3];
        float[] b = [1, 2, 3.1f];

        // Act
        float result = TextSearchHelper.CosineSimilarity(a, b);

        // Assert
        result.Should().BeApproximately(1.0f, 0.001f);
    }

    [Fact]
    public void CosineSimilarity_WithKnownComputedValues_ReturnsExpectedResult()
    {
        // Arrange
        // dot = 1*4 + 2*5 + 3*6 = 32
        // ||a|| = sqrt(14) ≈ 3.7417, ||b|| = sqrt(77) ≈ 8.7749
        // similarity = 32 / (3.7417 * 8.7749) ≈ 0.9746
        float[] a = [1, 2, 3];
        float[] b = [4, 5, 6];

        // Act
        float result = TextSearchHelper.CosineSimilarity(a, b);

        // Assert
        result.Should().BeApproximately(0.9746f, 0.001f);
    }
}
