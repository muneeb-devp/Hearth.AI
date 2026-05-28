using Hearth.Rag.VectorStore;
using Microsoft.Extensions.AI;

namespace Hearth.Rag.Pipeline;

public sealed record RagResult(
    string Answer,
    IReadOnlyList<VectorSearchResult> Sources,
    ChatResponse RawResponse);
