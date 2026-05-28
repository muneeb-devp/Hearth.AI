namespace Hearth.Rag.Documents;

public sealed class DocumentLoaderRegistry
{
    private readonly IEnumerable<IDocumentLoader> _loaders;

    public DocumentLoaderRegistry(IEnumerable<IDocumentLoader> loaders)
    {
        _loaders = loaders;
    }

    public IDocumentLoader GetLoader(string path)
    {
        return _loaders.FirstOrDefault(l => l.CanLoad(path))
            ?? throw new NotSupportedException(
                $"No document loader registered for '{Path.GetExtension(path)}'. " +
                $"Supported extensions: .txt, .md, .html");
    }

    public Task<IDocument> LoadAsync(string path, CancellationToken ct = default) =>
        GetLoader(path).LoadAsync(path, ct);
}
