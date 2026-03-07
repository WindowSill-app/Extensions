namespace WindowSill.TextFinder.Core.MeaningSimilarity;

/// <summary>
/// Tokenized input ready for the transformer model.
/// </summary>
/// <param name="InputIds">Token IDs from the vocabulary. Includes [CLS] at start and [SEP] at end.</param>
/// <param name="AttentionMask">Mask indicating real tokens (1) vs padding (0).</param>
/// <param name="TokenTypeIds">Segment IDs (all zeros for single-sentence input).</param>
internal readonly record struct EncodedInput(long[] InputIds, long[] AttentionMask, long[] TokenTypeIds);
