namespace Hearth.Tests;

public sealed class HearthOptionsTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var options = new HearthOptions();

        Assert.Null(options.Model);
        Assert.Null(options.ModelFile);
        Assert.Equal(4096, options.ContextSize);
        Assert.Equal(0, options.GpuLayers);
        Assert.Equal(512, options.BatchSize);
        Assert.Equal(-1, options.Threads);
        Assert.Null(options.CacheDirectory);
        Assert.Null(options.HuggingFaceToken);
        Assert.Null(options.OnDownloadProgress);
    }

    [Fact]
    public void Properties_RoundTrip()
    {
        var options = new HearthOptions
        {
            Model = "/path/to/model.gguf",
            ModelFile = "model.gguf",
            ContextSize = 8192,
            GpuLayers = 35,
            BatchSize = 256,
            Threads = 8,
            CacheDirectory = "./models"
        };

        Assert.Equal("/path/to/model.gguf", options.Model);
        Assert.Equal("model.gguf", options.ModelFile);
        Assert.Equal(8192, options.ContextSize);
        Assert.Equal(35, options.GpuLayers);
        Assert.Equal(256, options.BatchSize);
        Assert.Equal(8, options.Threads);
        Assert.Equal("./models", options.CacheDirectory);
    }

    [Fact]
    public void GpuLayers_CanBeZeroForCpuOnly()
    {
        var options = new HearthOptions { GpuLayers = 0 };
        Assert.Equal(0, options.GpuLayers);
    }

    [Fact]
    public void GpuLayers_LargeValueRepresentsAllLayers()
    {
        var options = new HearthOptions { GpuLayers = 999 };
        Assert.Equal(999, options.GpuLayers);
    }
}
