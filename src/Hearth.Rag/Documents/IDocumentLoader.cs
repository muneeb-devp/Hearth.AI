namespace Hearth.Rag.Documents;

public interface IDocumentLoader
{
    bool CanLoad(string path);
    Task<IDocument> LoadAsync(string path, CancellationToken ct = default);
}
