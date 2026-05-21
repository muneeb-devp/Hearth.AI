using Microsoft.Extensions.AI;

namespace Hearth.AspNetCore.Tests.Mocks;

public sealed class MockEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata { get; } = new("hearth-test", null, "mock-model", 4);

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = values
            .Select(static (_, i) => new Embedding<float>(new float[] { 0.1f * (i + 1), 0.2f, 0.3f, 0.4f }))
            .ToList();

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
