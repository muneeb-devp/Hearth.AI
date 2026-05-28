using HtmlAgilityPack;

namespace Hearth.Rag.Documents;

public sealed class HtmlLoader : IDocumentLoader
{
    private static readonly string[] Extensions = [".html", ".htm"];

    public bool CanLoad(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public async Task<IDocument> LoadAsync(string path, CancellationToken ct = default)
    {
        var html = await File.ReadAllTextAsync(path, ct);
        var content = ExtractText(html);
        return new PlainDocument { Content = content, Source = path };
    }

    internal static string ExtractText(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove script and style nodes
        foreach (var node in doc.DocumentNode
            .SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>())
        {
            node.Remove();
        }

        return System.Net.WebUtility.HtmlDecode(doc.DocumentNode.InnerText)
            .Trim();
    }
}
