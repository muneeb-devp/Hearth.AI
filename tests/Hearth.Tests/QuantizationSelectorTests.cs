namespace Hearth.Tests;

public sealed class QuantizationSelectorTests
{
    private static HuggingFaceFile File(string name, long size = 1_000_000) =>
        new() { FileName = name, Size = size };

    [Fact]
    public void SelectBest_EmptyList_ReturnsNull()
    {
        var result = QuantizationSelector.SelectBest([], null);

        Assert.Null(result);
    }

    [Fact]
    public void SelectBest_AllNonGguf_ReturnsNull()
    {
        var files = new List<HuggingFaceFile>
        {
            new() { FileName = "README.md" },
            new() { FileName = "config.json" },
            new() { FileName = "tokenizer.model" },
        };

        var result = QuantizationSelector.SelectBest(files, null);

        Assert.Null(result);
    }

    [Fact]
    public void SelectBest_FiltersOutNonGguf()
    {
        var files = new List<HuggingFaceFile>
        {
            new() { FileName = "README.md" },
            File("model-q4_k_m.gguf"),
        };

        var result = QuantizationSelector.SelectBest(files, null);

        Assert.Equal("model-q4_k_m.gguf", result?.FileName);
    }

    [Fact]
    public void SelectBest_PrefersQ4KM_OverLowerQuality()
    {
        var files = new List<HuggingFaceFile>
        {
            File("model-q8_0.gguf"),
            File("model-q4_k_m.gguf"),
            File("model-q2_k.gguf"),
        };

        var result = QuantizationSelector.SelectBest(files, null);

        Assert.Equal("model-q4_k_m.gguf", result?.FileName);
    }

    [Fact]
    public void SelectBest_FallsBackToQ5KM_WhenNoQ4KM()
    {
        var files = new List<HuggingFaceFile>
        {
            File("model-q2_k.gguf"),
            File("model-q5_k_m.gguf"),
            File("model-q8_0.gguf"),
        };

        var result = QuantizationSelector.SelectBest(files, null);

        Assert.Equal("model-q5_k_m.gguf", result?.FileName);
    }

    [Fact]
    public void SelectBest_Q4KM_BeatsQ5KM()
    {
        var files = new List<HuggingFaceFile>
        {
            File("model-q5_k_m.gguf"),
            File("model-q4_k_m.gguf"),
        };

        var result = QuantizationSelector.SelectBest(files, null);

        Assert.Equal("model-q4_k_m.gguf", result?.FileName);
    }

    [Fact]
    public void SelectBest_HonorsPreferredFile()
    {
        var files = new List<HuggingFaceFile>
        {
            File("model-q4_k_m.gguf"),
            File("model-q8_0.gguf"),
        };

        var result = QuantizationSelector.SelectBest(files, "model-q8_0.gguf");

        Assert.Equal("model-q8_0.gguf", result?.FileName);
    }

    [Fact]
    public void SelectBest_PreferredFileNotFound_ReturnsNull()
    {
        var files = new List<HuggingFaceFile>
        {
            File("model-q4_k_m.gguf"),
        };

        var result = QuantizationSelector.SelectBest(files, "nonexistent.gguf");

        Assert.Null(result);
    }

    [Fact]
    public void SelectBest_PreferredFileCaseInsensitive()
    {
        var files = new List<HuggingFaceFile>
        {
            File("Model-Q4_K_M.GGUF"),
        };

        var result = QuantizationSelector.SelectBest(files, "model-q4_k_m.gguf");

        Assert.Equal("Model-Q4_K_M.GGUF", result?.FileName);
    }

    [Fact]
    public void SelectBest_UnknownQuant_ReturnsItRatherThanNull()
    {
        var files = new List<HuggingFaceFile>
        {
            File("model-fp16.gguf"),
        };

        var result = QuantizationSelector.SelectBest(files, null);

        Assert.Equal("model-fp16.gguf", result?.FileName);
    }

    [Fact]
    public void SelectBest_PreferenceOrder_Q4KS_BeatsQ4_0()
    {
        var files = new List<HuggingFaceFile>
        {
            File("model-q4_0.gguf"),
            File("model-q4_k_s.gguf"),
        };

        var result = QuantizationSelector.SelectBest(files, null);

        Assert.Equal("model-q4_k_s.gguf", result?.FileName);
    }
}
