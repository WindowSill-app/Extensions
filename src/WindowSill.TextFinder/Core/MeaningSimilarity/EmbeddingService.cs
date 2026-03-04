using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.TextFinder.Core.MeaningSimilarity;

/// <summary>
/// Generates text embeddings using a local ONNX sentence transformer model (all-MiniLM-L6-v2).
/// </summary>
/// <remarks>
/// <para>
/// Embeddings are dense vector representations that capture semantic meaning.
/// Similar texts produce vectors that are close together in the embedding space.
/// </para>
/// <para>
/// The model expects three input tensors from the tokenizer:
/// <list type="bullet">
///   <item><b>input_ids</b>: Token IDs from the vocabulary (e.g., "hello" → 7592).</item>
///   <item><b>attention_mask</b>: Indicates real tokens (1) vs padding (0).</item>
///   <item><b>token_type_ids</b>: Segment IDs for sentence-pair tasks (all zeros for single sentences).</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class EmbeddingService : IDisposable
{
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingService"/> class.
    /// </summary>
    public EmbeddingService(IPluginInfo pluginInfo)
    {
        string modelDirectory = ModelManager.GetModelDirectory(pluginInfo.GetPluginDataFolder());
        string modelPath = Path.Combine(modelDirectory, ModelManager.ModelFileName);
        string vocabPath = Path.Combine(modelDirectory, ModelManager.VocabFileName);

        if (!ModelManager.ModelExists(modelDirectory))
        {
            throw new FileNotFoundException("Model files not found. Please download the model before initializing the EmbeddingService.");
        }

        var sessionOptions = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };

        _session = new InferenceSession(modelPath, sessionOptions);
        _tokenizer = new Tokenizer(vocabPath);
    }

    /// <summary>
    /// Generates a normalized embedding vector for the specified text.
    /// </summary>
    /// <param name="text">The input text to embed.</param>
    /// <returns>A normalized float array representing the text embedding.</returns>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    public float[] GetEmbedding(string text, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        EncodedInput encoded = _tokenizer.Encode(text, cancellationToken);

        var inputIds = new DenseTensor<long>(encoded.InputIds, [1, encoded.InputIds.Length]);
        var attentionMask = new DenseTensor<long>(encoded.AttentionMask, [1, encoded.AttentionMask.Length]);
        var tokenTypeIds = new DenseTensor<long>(encoded.TokenTypeIds, [1, encoded.TokenTypeIds.Length]);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
        };

        cancellationToken.ThrowIfCancellationRequested();

        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);

        Tensor<float> embeddings = results.First().AsTensor<float>();
        return MeanPooling(embeddings, encoded.AttentionMask, cancellationToken);
    }

    /// <summary>
    /// Applies mean pooling to collapse token-level embeddings into a single sentence embedding.
    /// </summary>
    /// <remarks>
    /// The transformer outputs one embedding per token. Mean pooling averages these vectors,
    /// weighted by attention_mask to exclude padding tokens from the calculation.
    /// </remarks>
    private static float[] MeanPooling(Tensor<float> embeddings, long[] attentionMask, CancellationToken cancellationToken)
    {
        int sequenceLength = attentionMask.Length;
        int embeddingDim = (int)(embeddings.Length / sequenceLength);
        float[] pooled = new float[embeddingDim];

        float maskSum = attentionMask.Sum();

        for (int i = 0; i < sequenceLength; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (attentionMask[i] == 0)
            {
                continue;
            }

            for (int j = 0; j < embeddingDim; j++)
            {
                pooled[j] += embeddings[0, i, j];
            }
        }

        for (int j = 0; j < embeddingDim; j++)
        {
            pooled[j] /= maskSum;
        }

        return Normalize(pooled);
    }

    /// <summary>
    /// Applies L2 normalization so the vector has unit length.
    /// </summary>
    /// <remarks>
    /// Normalizing allows cosine similarity to be computed as a simple dot product.
    /// </remarks>
    private static float[] Normalize(float[] vector)
    {
        float norm = MathF.Sqrt(vector.Sum(x => x * x));

        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] /= norm;
        }

        return vector;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.Dispose();
        _disposed = true;
    }
}
