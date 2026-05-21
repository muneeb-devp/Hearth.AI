namespace Hearth;

/// <summary>Selects the best GGUF file from a Hugging Face repository's file list.</summary>
internal static class QuantizationSelector
{
    // Ordered from most preferred to least preferred.
    // Q4_K_M is the sweet spot: ~40% size reduction with minimal quality loss.
    private static readonly string[] PreferenceOrder =
    [
        "Q4_K_M", "Q5_K_M", "Q4_K_S", "Q5_K_S", "Q4_K_L",
        "Q4_0",   "Q5_0",   "Q6_K",   "Q8_0",
        "Q3_K_M", "Q3_K_S", "Q3_K_L",
        "Q2_K",
    ];

    /// <summary>
    /// Returns the best GGUF file from <paramref name="files"/>.
    /// If <paramref name="preferredFile"/> is specified, that exact filename is returned (or
    /// <see langword="null"/> if it is not present). Otherwise the file is scored by quantization
    /// preference and the highest-ranked result is returned.
    /// </summary>
    internal static HuggingFaceFile? SelectBest(
        IReadOnlyList<HuggingFaceFile> files,
        string? preferredFile)
    {
        var ggufFiles = files
            .Where(static f => f.FileName.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (ggufFiles.Count == 0)
        {
            return null;
        }

        if (preferredFile is not null)
        {
            return ggufFiles.FirstOrDefault(
                f => f.FileName.Equals(preferredFile, StringComparison.OrdinalIgnoreCase));
        }

        return ggufFiles
            .Select(f => (File: f, Score: Score(f.FileName)))
            .OrderBy(static x => x.Score < 0 ? int.MaxValue : x.Score)
            .Select(static x => x.File)
            .First();
    }

    private static int Score(string filename)
    {
        var upper = filename.ToUpperInvariant();
        for (var i = 0; i < PreferenceOrder.Length; i++)
        {
            if (upper.Contains(PreferenceOrder[i], StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
