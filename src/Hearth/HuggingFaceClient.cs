using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Hearth;

/// <summary>Minimal Hugging Face Hub client for listing model files and downloading them.</summary>
internal sealed class HuggingFaceClient : IDisposable
{
    private const string ApiBase = "https://huggingface.co/api/models/";
    private const string ResolveBase = "https://huggingface.co/";
    private const int BufferSize = 81_920;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;

    internal HuggingFaceClient(string? token)
    {
        _http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Hearth/0.1 (LLamaSharp; dotnet)");

        if (token is not null)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>Returns the list of files in a Hugging Face model repository.</summary>
    internal async Task<IReadOnlyList<HuggingFaceFile>> ListFilesAsync(
        string repoId,
        CancellationToken ct)
    {
        var url = ApiBase + repoId;
        using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"Hugging Face repository '{repoId}' was not found. " +
                "Verify the repo ID, or set HearthOptions.HuggingFaceToken if the repo is private.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(
                $"Access denied for Hugging Face repository '{repoId}'. " +
                "Set HearthOptions.HuggingFaceToken to a valid access token.");
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var info = JsonSerializer.Deserialize<HuggingFaceModelInfo>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                $"Failed to parse model metadata for '{repoId}'.");

        return info.Siblings;
    }

    /// <summary>
    /// Downloads a file from a Hugging Face repository, resuming from an existing partial file if present.
    /// Renames the completed file from <c>{destPath}.tmp</c> to <paramref name="destPath"/> atomically.
    /// </summary>
    internal async Task DownloadFileAsync(
        string repoId,
        string filename,
        string destPath,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken ct)
    {
        var downloadUrl = $"{ResolveBase}{repoId}/resolve/main/{Uri.EscapeDataString(filename)}";
        var tmpPath = destPath + ".tmp";

        var existingBytes = File.Exists(tmpPath) ? new FileInfo(tmpPath).Length : 0L;

        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        if (existingBytes > 0)
        {
            request.Headers.Range = new RangeHeaderValue(existingBytes, null);
        }

        using var response = await _http
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        // 416 = server says our range is already satisfied — restart from scratch
        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            existingBytes = 0;
            using var retryRequest = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            using var retryResponse = await _http
                .SendAsync(retryRequest, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);
            retryResponse.EnsureSuccessStatusCode();
            await WriteToFileAsync(retryResponse, filename, tmpPath, destPath, 0, false, progress, ct)
                .ConfigureAwait(false);
            return;
        }

        response.EnsureSuccessStatusCode();

        var isPartial = response.StatusCode == HttpStatusCode.PartialContent;
        await WriteToFileAsync(response, filename, tmpPath, destPath, existingBytes, isPartial, progress, ct)
            .ConfigureAwait(false);
    }

    private static async Task WriteToFileAsync(
        HttpResponseMessage response,
        string filename,
        string tmpPath,
        string destPath,
        long resumeOffset,
        bool isPartial,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken ct)
    {
        var remoteLength = response.Content.Headers.ContentLength ?? -1L;
        var totalBytes = isPartial && remoteLength >= 0 ? resumeOffset + remoteLength : remoteLength;
        var fileMode = isPartial && resumeOffset > 0 ? FileMode.Append : FileMode.Create;

        await using var fileStream = new FileStream(
            tmpPath, fileMode, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        await using var networkStream = await response.Content
            .ReadAsStreamAsync(ct)
            .ConfigureAwait(false);

        var buffer = new byte[BufferSize];
        var downloaded = resumeOffset;
        int read;

        while ((read = await networkStream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            downloaded += read;
            progress?.Report(new ModelDownloadProgress
            {
                FileName = filename,
                BytesDownloaded = downloaded,
                TotalBytes = totalBytes,
                IsResumed = resumeOffset > 0,
            });
        }

        await fileStream.FlushAsync(ct).ConfigureAwait(false);

        // Atomic rename: partial file → final destination
        File.Move(tmpPath, destPath, overwrite: true);
    }

    public void Dispose() => _http.Dispose();
}
