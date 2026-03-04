using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using FuzzySharp;
using WindowSill.TextFinder.Core.MeaningSimilarity;

namespace WindowSill.TextFinder.Core;

internal static partial class TextSearchHelper
{
    private const int ExtractLengthBeforeMatch = 30;
    private const int ExtractLengthAfterMatch = 300;

    public static ObservableCollection<FindResult> FindMatches(
        string sourceText,
        string searchText,
        SearchMode searchMode,
        bool matchCase,
        bool matchWholeWord,
        float minimumScore,
        Lazy<EmbeddingService> embeddingService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sourceText) || string.IsNullOrEmpty(searchText))
        {
            return [];
        }

        IReadOnlyList<TextSpan> matches = searchMode switch
        {
            SearchMode.LiteralMatch => FindLiteralMatches(sourceText, searchText, matchCase, matchWholeWord, cancellationToken),
            SearchMode.RegularExpression => FindRegexMatches(sourceText, searchText, matchCase, cancellationToken),
            SearchMode.SpellingSimilarity => FindFuzzyMatches(sourceText, searchText, minimumScore: 70, cancellationToken),
            SearchMode.MeaningSimilarity => FindMeaningMatches(sourceText, searchText, minimumScore, embeddingService, cancellationToken),
            _ => throw new ArgumentException($"Unsupported search mode: {searchMode}", nameof(searchMode)),
        };

        var results = new ObservableCollection<FindResult>();
        foreach (TextSpan match in matches)
        {
            if (match.Length == 0)
            {
                continue;
            }

            FindResult result = BuildFindResult(sourceText, match);
            results.Add(result);
        }

        return results;
    }

    private static List<TextSpan> FindLiteralMatches(
        string sourceText,
        string searchText,
        bool matchCase,
        bool matchWholeWord,
        CancellationToken cancellationToken)
    {
        string escapedPattern = Regex.Escape(searchText);

        if (matchWholeWord)
        {
            escapedPattern = $@"\b{escapedPattern}\b";
        }

        RegexOptions options = RegexOptions.Compiled;
        if (!matchCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        return ToTextMatches(Regex.Matches(sourceText, escapedPattern, options), cancellationToken);
    }

    private static List<TextSpan> FindRegexMatches(
        string sourceText,
        string searchText,
        bool matchCase,
        CancellationToken cancellationToken)
    {
        if (!IsValidRegex(searchText, matchCase ? RegexOptions.None : RegexOptions.IgnoreCase))
        {
            return [];
        }

        RegexOptions options = RegexOptions.Compiled;
        if (!matchCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        return ToTextMatches(Regex.Matches(sourceText, searchText, options), cancellationToken);
    }

    private static List<TextSpan> FindFuzzyMatches(
        string sourceText,
        string searchText,
        int minimumScore,
        CancellationToken cancellationToken)
    {
        var results = new List<TextSpan>();
        var wordPattern = new Regex(@"\b\w+\b", RegexOptions.Compiled);

        foreach (Match wordMatch in wordPattern.Matches(sourceText))
        {
            cancellationToken.ThrowIfCancellationRequested();
            int score = Fuzz.Ratio(searchText, wordMatch.Value);
            if (score >= minimumScore)
            {
                results.Add(new TextSpan(wordMatch.Index, wordMatch.Length));
            }
        }

        return results;
    }

    private static List<TextSpan> FindMeaningMatches(
        string sourceText,
        string searchText,
        float minimumScore,
        Lazy<EmbeddingService> embeddingService,
        CancellationToken cancellationToken)
    {
        var results = new List<TextSpan>();

        Regex sentencePattern = SentenceRegex();

        float[] queryEmbedding = embeddingService.Value.GetEmbedding(searchText, cancellationToken);

        foreach (Match sentence in sentencePattern.Matches(sourceText))
        {
            cancellationToken.ThrowIfCancellationRequested();
            float[] sentenceEmbedding = embeddingService.Value.GetEmbedding(sentence.Value, cancellationToken);
            float similarity = CosineSimilarity(queryEmbedding, sentenceEmbedding);

            if (similarity >= minimumScore)
            {
                results.Add(new TextSpan(sentence.Index, sentence.Length));
            }
        }

        return results;
    }

    private static List<TextSpan> ToTextMatches(MatchCollection regexMatches, CancellationToken cancellationToken)
    {
        var results = new List<TextSpan>(regexMatches.Count);
        foreach (Match match in regexMatches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(new TextSpan(match.Index, match.Length));
        }

        return results;
    }

    private static FindResult BuildFindResult(string sourceText, TextSpan match)
    {
        int extractStart = Math.Max(0, match.Index - ExtractLengthBeforeMatch);
        int extractEnd = Math.Min(sourceText.Length, match.Index + match.Length + ExtractLengthAfterMatch);
        int extractLen = extractEnd - extractStart;

        string rawExtract = sourceText.Substring(extractStart, extractLen);
        string normalizedExtract = Regex.Replace(rawExtract, @"\r\n|\r|\n", "⏎");

        string prefix = extractStart > 0 ? "..." : string.Empty;
        string suffix = extractEnd < sourceText.Length ? "..." : string.Empty;

        string extract = $"{prefix}{normalizedExtract}{suffix}";
        int matchStartInExtract = prefix.Length + (match.Index - extractStart);

        return new FindResult
        {
            Match = match,
            PreviewText = extract,
            MatchInPreview = new TextSpan(matchStartInExtract, match.Length)
        };
    }

    private static bool IsValidRegex(string pattern, RegexOptions options = RegexOptions.None)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        try
        {
            _ = new Regex(pattern, options);
            return true;
        }
        catch (RegexParseException)
        {
            return false;
        }
    }

    /// <summary>
    /// Computes cosine similarity between two vectors.
    /// </summary>
    /// <remarks>
    /// Returns a value between -1 and 1, where 1 means identical direction (semantically similar),
    /// 0 means orthogonal (unrelated), and -1 means opposite.
    /// </remarks>
    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0f, normA = 0f, normB = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    [GeneratedRegex(@"[^.!?]*[.!?]", RegexOptions.Compiled)]
    private static partial Regex SentenceRegex();
}
