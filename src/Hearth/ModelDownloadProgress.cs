namespace Hearth;

/// <summary>Progress update emitted during a Hugging Face model download.</summary>
public readonly record struct ModelDownloadProgress
{
    /// <summary>The filename being downloaded.</summary>
    public string FileName { get; init; }

    /// <summary>Bytes downloaded so far, including bytes from a resumed partial download.</summary>
    public long BytesDownloaded { get; init; }

    /// <summary>Total expected bytes, or <c>-1</c> if the server did not provide a Content-Length.</summary>
    public long TotalBytes { get; init; }

    /// <summary>Whether this transfer was resumed from an existing partial file.</summary>
    public bool IsResumed { get; init; }

    /// <summary>Completion percentage, or <see cref="double.NaN"/> when total size is unknown.</summary>
    public double Percentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100.0 : double.NaN;
}
