namespace Hearth.Rag.Pipeline;

internal static class RagDefaults
{
    internal const string ContextTemplate =
        """
        You are a helpful assistant. Answer the question using ONLY the context below.
        If the answer is not in the context, say "I don't have enough information to answer that."

        Context:
        {0}
        """;
}
