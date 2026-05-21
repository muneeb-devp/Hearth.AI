namespace Hearth;

/// <summary>Configuration options for Hearth local LLM inference.</summary>
public sealed class HearthOptions
{
    /// <summary>
    /// Path to a local GGUF model file.
    /// Example: <c>./models/qwen2.5-7b-q4_k_m.gguf</c>
    /// </summary>
    /// <remarks>
    /// Phase 2 will add Hugging Face repository ID support
    /// (e.g. <c>Qwen/Qwen2.5-7B-Instruct-GGUF</c>).
    /// </remarks>
    public string? Model { get; set; }

    /// <summary>
    /// Specific GGUF file within a model repository. Ignored when
    /// <see cref="Model"/> is already a direct file path.
    /// </summary>
    public string? ModelFile { get; set; }

    /// <summary>
    /// Maximum context window size in tokens. Defaults to <c>4096</c>.
    /// Larger values require proportionally more VRAM/RAM.
    /// </summary>
    public int ContextSize { get; set; } = 4096;

    /// <summary>
    /// Number of model layers to offload to GPU. Set to <c>0</c> for CPU-only
    /// inference; set to a large number (e.g. <c>999</c>) to offload all layers.
    /// Defaults to <c>0</c>.
    /// </summary>
    public int GpuLayers { get; set; } = 0;

    /// <summary>
    /// Directory for caching downloaded models.
    /// Defaults to <c>~/.hearth/models</c> when <see langword="null"/>.
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Batch size for prompt processing. Higher values can improve throughput at
    /// the cost of memory. Defaults to <c>512</c>.
    /// </summary>
    public int BatchSize { get; set; } = 512;

    /// <summary>
    /// Number of CPU threads for inference. <c>-1</c> lets the runtime choose
    /// based on available cores. Defaults to <c>-1</c>.
    /// </summary>
    public int Threads { get; set; } = -1;

    /// <summary>
    /// Hugging Face API access token. Required for private or gated repositories.
    /// Generate one at <c>https://huggingface.co/settings/tokens</c>.
    /// </summary>
    public string? HuggingFaceToken { get; set; }

    /// <summary>
    /// Callback invoked with progress updates during a Hugging Face model download.
    /// Only called when <see cref="Model"/> is a Hugging Face repository ID and the
    /// file is not already cached.
    /// </summary>
    /// <example>
    /// <code>
    /// options.OnDownloadProgress = p =>
    ///     Console.WriteLine($"Downloading {p.FileName}: {p.Percentage:F1}%");
    /// </code>
    /// </example>
    public Action<ModelDownloadProgress>? OnDownloadProgress { get; set; }
}
