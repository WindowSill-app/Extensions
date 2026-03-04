namespace WindowSill.TextFinder.Core.MeaningSimilarity;

/// <summary>
/// WordPiece tokenizer for BERT-based transformer models.
/// </summary>
/// <remarks>
/// <para>
/// WordPiece splits words into subword units from a fixed vocabulary.
/// Unknown words are broken into known pieces (e.g., "embeddings" → "em", "##bed", "##ding", "##s").
/// The "##" prefix indicates a continuation of the previous token.
/// </para>
/// <para>
/// BERT models require special tokens:
/// <list type="bullet">
///   <item><b>[CLS]</b> (101): Classification token at sequence start.</item>
///   <item><b>[SEP]</b> (102): Separator token at sequence end.</item>
///   <item><b>[PAD]</b> (0): Padding to fill fixed-length sequences.</item>
///   <item><b>[UNK]</b> (100): Unknown token for out-of-vocabulary words.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class Tokenizer
{
    private readonly Dictionary<string, int> _vocabulary;
    private readonly Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> _vocabularyLookup;
    private const int MaxSequenceLength = 128;
    private const int ClsTokenId = 101;   // [CLS] - start of sequence
    private const int SepTokenId = 102;   // [SEP] - end of sequence
    private const int PadTokenId = 0;     // [PAD] - padding
    private const int UnknownTokenId = 100; // [UNK] - unknown token

    /// <summary>
    /// Loads the WordPiece vocabulary from a text file.
    /// </summary>
    /// <param name="vocabularyPath">Path to vocab.txt with one token per line.</param>
    public Tokenizer(string vocabularyPath)
    {
        _vocabulary = File.ReadAllLines(vocabularyPath)
            .Select((token, index) => (token, index))
            .ToDictionary(x => x.token, x => x.index);
        _vocabularyLookup = _vocabulary.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    /// <summary>
    /// Converts text into model input tensors.
    /// </summary>
    /// <param name="text">The input text to tokenize.</param>
    /// <param name="cancellationToken">Token to cancel the encoding operation.</param>
    /// <returns>Encoded input containing input_ids, attention_mask, and token_type_ids arrays.</returns>
    public EncodedInput Encode(string text, CancellationToken cancellationToken)
    {
        long[] inputIds = new long[MaxSequenceLength];
        long[] attentionMask = new long[MaxSequenceLength];
        long[] tokenTypeIds = new long[MaxSequenceLength];

        // Reserve slot 0 for [CLS].
        int tokenCount = 1;
        inputIds[0] = ClsTokenId;
        attentionMask[0] = 1;

        ReadOnlySpan<char> lowerText = text.AsSpan();
        Span<char> lowerBuffer = text.Length <= 256
            ? stackalloc char[text.Length]
            : new char[text.Length];
        lowerText.ToLowerInvariant(lowerBuffer);

        foreach (Range wordRange in lowerBuffer.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReadOnlySpan<char> word = lowerBuffer[wordRange];
            if (word.IsEmpty)
            {
                continue;
            }

            TokenizeWord(word, inputIds, attentionMask, ref tokenCount);

            if (tokenCount >= MaxSequenceLength - 1)
            {
                break;
            }
        }

        // Append [SEP].
        if (tokenCount < MaxSequenceLength)
        {
            inputIds[tokenCount] = SepTokenId;
            attentionMask[tokenCount] = 1;
            tokenCount++;
        }

        return new EncodedInput(inputIds, attentionMask, tokenTypeIds);
    }

    private void TokenizeWord(ReadOnlySpan<char> word, long[] inputIds, long[] attentionMask, ref int tokenCount)
    {
        // Buffer for "##" prefix + substring.
        int bufferLength = 2 + word.Length;
        char[] rentedBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(bufferLength);
        Span<char> prefixBuffer = rentedBuffer.AsSpan(0, bufferLength);
        prefixBuffer[0] = '#';
        prefixBuffer[1] = '#';

        int start = 0;

        while (start < word.Length && tokenCount < MaxSequenceLength - 1)
        {
            int end = word.Length;
            int foundTokenId = -1;

            while (start < end)
            {
                ReadOnlySpan<char> candidate;
                if (start > 0)
                {
                    ReadOnlySpan<char> slice = word[start..end];
                    slice.CopyTo(prefixBuffer[2..]);
                    candidate = prefixBuffer[..(2 + slice.Length)];
                }
                else
                {
                    candidate = word[start..end];
                }

                if (_vocabularyLookup.TryGetValue(candidate, out int id))
                {
                    foundTokenId = id;
                    break;
                }

                end--;
            }

            if (foundTokenId < 0)
            {
                inputIds[tokenCount] = UnknownTokenId;
                attentionMask[tokenCount] = 1;
                tokenCount++;
                break;
            }

            inputIds[tokenCount] = foundTokenId;
            attentionMask[tokenCount] = 1;
            tokenCount++;
            start = end;
        }

        System.Buffers.ArrayPool<char>.Shared.Return(rentedBuffer);
    }
}
