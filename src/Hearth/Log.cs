using Microsoft.Extensions.Logging;

namespace Hearth;

internal static partial class Log
{
    [LoggerMessage(1, LogLevel.Information, "Loading model from {ModelPath}")]
    internal static partial void LoadingModel(ILogger logger, string modelPath);

    [LoggerMessage(2, LogLevel.Information, "Model loaded in {ElapsedMs}ms")]
    internal static partial void ModelLoaded(ILogger logger, long elapsedMs);

    [LoggerMessage(3, LogLevel.Debug, "Starting inference, {MessageCount} messages in context")]
    internal static partial void StartingInference(ILogger logger, int messageCount);

    [LoggerMessage(4, LogLevel.Debug, "Inference complete, {TokenCount} tokens generated")]
    internal static partial void InferenceCompleted(ILogger logger, int tokenCount);

    [LoggerMessage(5, LogLevel.Error, "Inference failed after {TokenCount} tokens")]
    internal static partial void InferenceFailed(ILogger logger, int tokenCount, Exception exception);

    [LoggerMessage(6, LogLevel.Information, "Fetching model metadata from Hugging Face: {RepoId}")]
    internal static partial void FetchingModelInfo(ILogger logger, string repoId);

    [LoggerMessage(7, LogLevel.Information, "Selected quantization: {FileName}")]
    internal static partial void SelectedQuantization(ILogger logger, string fileName);

    [LoggerMessage(8, LogLevel.Information, "Model cache hit: {ModelPath}")]
    internal static partial void ModelCacheHit(ILogger logger, string modelPath);

    [LoggerMessage(9, LogLevel.Warning, "Cached model failed SHA-256 verification, re-downloading: {ModelPath}")]
    internal static partial void ModelCacheInvalid(ILogger logger, string modelPath);

    [LoggerMessage(10, LogLevel.Information, "Downloading {FileName} ({Bytes} bytes)")]
    internal static partial void StartingDownload(ILogger logger, string fileName, long bytes);

    [LoggerMessage(11, LogLevel.Information, "Download complete: {FileName}")]
    internal static partial void DownloadComplete(ILogger logger, string fileName);

    [LoggerMessage(12, LogLevel.Debug, "Invoking tool: {FunctionName}")]
    internal static partial void ToolCallStarted(ILogger logger, string functionName);

    [LoggerMessage(13, LogLevel.Debug, "Tool invocation complete: {FunctionName}")]
    internal static partial void ToolCallCompleted(ILogger logger, string functionName);

    [LoggerMessage(14, LogLevel.Warning, "Tool invocation failed: {FunctionName}")]
    internal static partial void ToolCallFailed(ILogger logger, string functionName, Exception exception);
}
