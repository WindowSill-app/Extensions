using System.Collections.ObjectModel;

using FluentAssertions;
using WindowSill.TextFinder.Core;
using WindowSill.TextFinder.Core.MeaningSimilarity;

namespace UnitTests.TextFinder;

/// <summary>
/// Unit tests for <see cref="TextSearchHelper"/> covering literal, regex, fuzzy,
/// edge-case, cancellation, and preview-building scenarios.
/// </summary>
public class TextSearchHelperTests
{
    private static readonly Lazy<EmbeddingService> DummyEmbeddingService =
        new(() => throw new NotImplementedException("EmbeddingService not available in tests"));

    private static ObservableCollection<FindResult> Find(
        string sourceText,
        string searchText,
        SearchMode mode,
        bool matchCase = false,
        bool matchWholeWord = false,
        float minimumScore = 0.7f,
        CancellationToken cancellationToken = default)
        => TextSearchHelper.FindMatches(
            sourceText, searchText, mode, matchCase, matchWholeWord,
            minimumScore, DummyEmbeddingService, cancellationToken);

    #region Empty / null inputs

    [Theory]
    [InlineData(null, "test")]
    [InlineData("", "test")]
    [InlineData("test", null)]
    [InlineData("test", "")]
    [InlineData("", "")]
    [InlineData(null, null)]
    internal void FindMatches_EmptyOrNullInputs_ReturnsEmptyCollection(string? sourceText, string? searchText)
    {
        ObservableCollection<FindResult> results = Find(sourceText!, searchText!, SearchMode.LiteralMatch);

        results.Should().BeEmpty();
    }

    #endregion

    #region Unsupported SearchMode

    [Fact]
    public void FindMatches_UnknownSearchMode_ThrowsArgumentException()
    {
        Action act = () => Find("hello", "hello", SearchMode.Unknown);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("searchMode");
    }

    #endregion

    #region Cancellation

    [Fact]
    public void FindMatches_CancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Action act = () => Find("hello world", "hello", SearchMode.LiteralMatch, cancellationToken: cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    #endregion

    #region LiteralMatch

    [Fact]
    public void LiteralMatch_BasicFind_ReturnsSingleMatch()
    {
        ObservableCollection<FindResult> results = Find("hello world", "world", SearchMode.LiteralMatch);

        results.Should().ContainSingle();
        results[0].Match.Index.Should().Be(6);
        results[0].Match.Length.Should().Be(5);
    }

    [Fact]
    public void LiteralMatch_MultipleMatches_ReturnsAll()
    {
        ObservableCollection<FindResult> results = Find("cat and cat", "cat", SearchMode.LiteralMatch);

        results.Should().HaveCount(2);
        results[0].Match.Index.Should().Be(0);
        results[1].Match.Index.Should().Be(8);
    }

    [Fact]
    public void LiteralMatch_NoMatch_ReturnsEmpty()
    {
        ObservableCollection<FindResult> results = Find("hello world", "xyz", SearchMode.LiteralMatch);

        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    internal void LiteralMatch_CaseSensitivity(bool matchCase, int expectedCount)
    {
        ObservableCollection<FindResult> results = Find("Hello hello", "hello", SearchMode.LiteralMatch, matchCase: matchCase);

        results.Should().HaveCount(expectedCount);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    internal void LiteralMatch_WholeWord(bool matchWholeWord, int expectedCount)
    {
        // "cat" appears as whole word and inside "catalog"
        ObservableCollection<FindResult> results = Find("cat catalog", "cat", SearchMode.LiteralMatch, matchWholeWord: matchWholeWord);

        results.Should().HaveCount(expectedCount);
    }

    #endregion

    #region RegularExpression

    [Fact]
    public void Regex_ValidPattern_ReturnsMatches()
    {
        ObservableCollection<FindResult> results = Find("abc 123 def 456", @"\d+", SearchMode.RegularExpression);

        results.Should().HaveCount(2);
        results[0].Match.Index.Should().Be(4);
        results[1].Match.Index.Should().Be(12);
    }

    [Fact]
    public void Regex_InvalidPattern_ReturnsEmpty()
    {
        ObservableCollection<FindResult> results = Find("hello", "[invalid", SearchMode.RegularExpression);

        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    internal void Regex_CaseSensitivity(bool matchCase, int expectedCount)
    {
        ObservableCollection<FindResult> results = Find("Abc abc", "abc", SearchMode.RegularExpression, matchCase: matchCase);

        results.Should().HaveCount(expectedCount);
    }

    #endregion

    #region SpellingSimilarity (Fuzzy)

    [Fact]
    public void Fuzzy_FindsSimilarWords()
    {
        // "helo" is close to "hello" (high Fuzz.Ratio)
        ObservableCollection<FindResult> results = Find("hello world", "helo", SearchMode.SpellingSimilarity);

        results.Should().ContainSingle();
        results[0].Match.Index.Should().Be(0);
    }

    [Fact]
    public void Fuzzy_DissimilarWord_ReturnsEmpty()
    {
        ObservableCollection<FindResult> results = Find("hello world", "zzzzz", SearchMode.SpellingSimilarity);

        results.Should().BeEmpty();
    }

    #endregion

    #region Zero-length matches

    [Fact]
    public void Regex_ZeroLengthMatches_AreFiltered()
    {
        // Lookahead produces zero-length matches
        ObservableCollection<FindResult> results = Find("abc", "(?=a)", SearchMode.RegularExpression);

        results.Should().BeEmpty();
    }

    #endregion

    #region BuildFindResult preview

    [Fact]
    public void BuildFindResult_PreviewContainsMatchText()
    {
        ObservableCollection<FindResult> results = Find("hello world", "world", SearchMode.LiteralMatch);

        FindResult result = results[0];
        result.PreviewText.Should().Contain("world");
    }

    [Fact]
    public void BuildFindResult_MatchInPreviewPositionIsCorrect()
    {
        ObservableCollection<FindResult> results = Find("hello world", "world", SearchMode.LiteralMatch);

        FindResult result = results[0];
        string extracted = result.PreviewText.Substring(result.MatchInPreview.Index, result.MatchInPreview.Length);
        extracted.Should().Be("world");
    }

    [Fact]
    public void BuildFindResult_LongSourceText_PreviewHasEllipsis()
    {
        // Create source text long enough to trigger prefix and suffix ellipsis
        string prefix = new('a', 100);
        string suffix = new('b', 500);
        string source = $"{prefix}MATCH{suffix}";

        ObservableCollection<FindResult> results = Find(source, "MATCH", SearchMode.LiteralMatch);

        FindResult result = results[0];
        result.PreviewText.Should().StartWith("...");
        result.PreviewText.Should().EndWith("...");
    }

    #endregion
}
