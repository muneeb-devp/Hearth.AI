using System.Diagnostics;
using LLama;
using LLama.Common;
using Microsoft.Extensions.Logging;

namespace Hearth;

/// <summary>
/// Holds the loaded <see cref="LLamaWeights"/> for the lifetime of the application.
/// Registered as a singleton; construction triggers model loading (and, if needed, downloading).
/// </summary>
internal sealed class HearthModel : IDisposable
{
    private readonly LLamaWeights _weights;

    internal LLamaWeights Weights => _weights;
    internal ModelParams ModelParams { get; }
    internal string ModelPath { get; }

    private HearthModel(LLamaWeights weights, ModelParams modelParams, string modelPath)
    {
        _weights = weights;
        ModelParams = modelParams;
        ModelPath = modelPath;
    }

    /// <summary>Creates a stateless executor that allocates a fresh KV-cache per inference call.</summary>
    internal StatelessExecutor CreateExecutor() => new(_weights, ModelParams, null!);

    /// <summary>Creates context params suitable for embedding generation (Embeddings flag enabled).</summary>
    internal ModelParams CreateEmbeddingParams() => new(ModelPath)
    {
        ContextSize = ModelParams.ContextSize,
        GpuLayerCount = ModelParams.GpuLayerCount,
        BatchSize = ModelParams.BatchSize,
        Embeddings = true,
    };

    /// <summary>
    /// Synchronous entry point used by the DI factory. Blocks while resolving and loading the model.
    /// Downloads from Hugging Face if <see cref="HearthOptions.Model"/> is a repository ID.
    /// </summary>
    internal static HearthModel Load(HearthOptions options, ILogger logger) =>
        LoadAsync(options, logger, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Async implementation — resolves the model path then loads weights into memory.</summary>
    internal static async Task<HearthModel> LoadAsync(
        HearthOptions options,
        ILogger logger,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(options);

        var modelPath = await ModelResolver.ResolveAsync(options, logger, ct).ConfigureAwait(false);

        Log.LoadingModel(logger, modelPath);
        var sw = Stopwatch.StartNew();

        var modelParams = BuildModelParams(options, modelPath);
        var weights = LLamaWeights.LoadFromFile(modelParams);

        sw.Stop();
        Log.ModelLoaded(logger, sw.ElapsedMilliseconds);

        return new HearthModel(weights, modelParams, modelPath);
    }

    private static ModelParams BuildModelParams(HearthOptions options, string modelPath)
    {
        var p = new ModelParams(modelPath)
        {
            ContextSize = (uint)options.ContextSize,
            GpuLayerCount = options.GpuLayers,
            BatchSize = (uint)options.BatchSize,
        };

        if (options.Threads > 0)
        {
            p.Threads = options.Threads;
        }

        return p;
    }

    public void Dispose() => _weights.Dispose();
}
