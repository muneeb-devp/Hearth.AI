using System.Security.Cryptography; using Microsoft.Extensions.Logging;

namespace Hearth;

/// <summary>
/// Resolves a model path: returns the local path as-is, or downloads from
/// Hugging Face and returns the cached path.
/// </summary>
internal static class ModelResolver
{
    internal static async Task<string> ResolveAsync(
        HearthOptions options,
        ILogger logger,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Model, nameof(options) + "." + nameof(options.Model));

        if (IsLocalPath(options.Model))
        {
            return ResolveLocalPath(options);
        }

        return await ResolveHuggingFaceAsync(options, logger, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="model"/> looks like a local file path
    /// rather than a Hugging Face repository ID (<c>owner/repo</c>).
    /// </summary>
    internal static bool IsLocalPath(string model)
    {
        return model.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)
            || model.StartsWith('.')
            || model.StartsWith('~')
            || model.Contains('\\')
            || Path.IsPathRooted(model)
            || File.Exists(model);
    }

    private static string ResolveLocalPath(HearthOptions options)
    {
        if (File.Exists(options.Model!))
        {
            return Path.GetFullPath(options.Model!);
        }

        if (options.CacheDirectory is not null && options.ModelFile is not null)
        {
            var cached = Path.Combine(options.CacheDirectory, options.ModelFile);
            if (File.Exists(cached))
            {
                return Path.GetFullPath(cached);
            }
        }

        throw new FileNotFoundException(
            $"Model file not found: '{options.Model}'. " +
            "Provide a valid local path or a Hugging Face repository ID (format: owner/repo-name).",
            options.Model);
    }

    private static async Task<string> ResolveHuggingFaceAsync(
        HearthOptions options,
        ILogger logger,
        CancellationToken ct)
    {
        var cacheDir = ResolveCacheDirectory(options);
        Directory.CreateDirectory(cacheDir);

        using var client = new HuggingFaceClient(options.HuggingFaceToken);

        Log.FetchingModelInfo(logger, options.Model!);
        var files = await client.ListFilesAsync(options.Model!, ct).ConfigureAwait(false);

        var selected = QuantizationSelector.SelectBest(files, options.ModelFile);
        if (selected is null)
        {
            var detail = options.ModelFile is not null
                ? $" Requested file '{options.ModelFile}' was not found in the repository."
                : string.Empty;
            throw new InvalidOperationException(
                $"No GGUF files found in Hugging Face repository '{options.Model}'.{detail}");
        }

        Log.SelectedQuantization(logger, selected.FileName);

        var destPath = Path.Combine(cacheDir, selected.FileName);

        if (File.Exists(destPath))
        {
            if (await VerifySha256Async(destPath, selected.Lfs?.Sha256, ct).ConfigureAwait(false))
            {
                Log.ModelCacheHit(logger, destPath);
                return destPath;
            }

            // Hash mismatch — corrupt or partial; delete and re-download
            Log.ModelCacheInvalid(logger, destPath);
            File.Delete(destPath);
        }

        var progress = options.OnDownloadProgress is not null
            ? new Progress<ModelDownloadProgress>(options.OnDownloadProgress)
            : null;

        Log.StartingDownload(logger, selected.FileName, selected.Lfs?.Size ?? selected.Size);

        await client.DownloadFileAsync(options.Model!, selected.FileName, destPath, progress, ct)
            .ConfigureAwait(false);

        Log.DownloadComplete(logger, selected.FileName);

        if (selected.Lfs?.Sha256 is string expectedHash)
        {
            if (!await VerifySha256Async(destPath, expectedHash, ct).ConfigureAwait(false))
            {
                File.Delete(destPath);
                throw new InvalidDataException(
                    $"SHA-256 verification failed for '{selected.FileName}'. " +
                    "The downloaded file may be corrupt. Delete the cache and try again.");
            }
        }

        return destPath;
    }

    private static async Task<bool> VerifySha256Async(
        string path,
        string? expectedHash,
        CancellationToken ct)
    {
        if (expectedHash is null)
        {
            return true;
        }

        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveCacheDirectory(HearthOptions options)
    {
        if (options.CacheDirectory is not null)
        {
            return options.CacheDirectory;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".hearth",
            "models");
    }
}
