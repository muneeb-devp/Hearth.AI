namespace Hearth.Rag.Documents;

public sealed class PlainTextLoader : IDocumentLoader
{
    private static readonly string[] Extensions = [".txt", ".text"];

    public bool CanLoad(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public async Task<IDocument> LoadAsync(string path, CancellationToken ct = default)
    {
        var content = await File.ReadAllTextAsync(path, ct);
        return new PlainDocument { Content = content, Source = path };
    }
}
