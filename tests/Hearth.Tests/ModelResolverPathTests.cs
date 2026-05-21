namespace Hearth.Tests;

public sealed class ModelResolverPathTests
{
    [Theory]
    [InlineData("./models/model.gguf")]
    [InlineData("../models/model.gguf")]
    [InlineData("/absolute/path/model.gguf")]
    [InlineData("model.gguf")]
    [InlineData("some-model.GGUF")]
    [InlineData(@"C:\Users\user\model.gguf")]
    [InlineData(@".\relative\model.gguf")]
    [InlineData("~/models/model.gguf")]
    public void IsLocalPath_RecognizesLocalPaths(string path)
    {
        Assert.True(ModelResolver.IsLocalPath(path));
    }

    [Theory]
    [InlineData("Qwen/Qwen2.5-7B-Instruct-GGUF")]
    [InlineData("microsoft/phi-3-mini-4k-instruct")]
    [InlineData("meta-llama/Meta-Llama-3-8B-Instruct")]
    [InlineData("TheBloke/Mistral-7B-Instruct-v0.3-GGUF")]
    [InlineData("bartowski/gemma-2-9b-it-GGUF")]
    [InlineData("deepseek-ai/DeepSeek-R1-Distill-Qwen-7B")]
    public void IsLocalPath_RecognizesHuggingFaceRepoIds(string repoId)
    {
        Assert.False(ModelResolver.IsLocalPath(repoId));
    }
}
