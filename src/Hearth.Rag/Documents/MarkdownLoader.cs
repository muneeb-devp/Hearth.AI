namespace Hearth.Rag.Documents;

public sealed class MarkdownLoader : IDocumentLoader
{
    private static readonly string[] Extensions = [".md", ".markdown"];

    public bool CanLoad(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public async Task<IDocument> LoadAsync(string path, CancellationToken ct = default)
    {
        var content = await File.ReadAllTextAsync(path, ct);
        content = StripFrontMatter(content);
        return new PlainDocument { Content = content, Source = path };
    }

    internal static string StripFrontMatter(string content)
    {
        if (!content.StartsWith("---", StringComparison.Ordinal))
        {
            return content;
        }

        var end = content.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
        {
            return content;
        }

        return content[(end + 4)..].TrimStart();
    }
}
