using Hearth.Rag.Documents;

namespace Hearth.Rag.Pipeline;

public interface IRagPipeline
{
    Task IndexAsync(string text, object? metadata = null, CancellationToken ct = default);
    Task IndexDocumentAsync(IDocument document, CancellationToken ct = default);
    Task<RagResult> AskAsync(string question, RagQueryOptions? options = null, CancellationToken ct = default);
}
