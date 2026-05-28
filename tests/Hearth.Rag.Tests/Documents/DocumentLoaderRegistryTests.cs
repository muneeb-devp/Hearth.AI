namespace Hearth.Rag.Tests.Documents;

public sealed class DocumentLoaderRegistryTests
{
    private static DocumentLoaderRegistry BuildRegistry() =>
        new([new PlainTextLoader(), new MarkdownLoader(), new HtmlLoader()]);

    [Theory]
    [InlineData("file.txt")]
    [InlineData("file.text")]
    public void GetLoader_TextFile_ReturnsPlainTextLoader(string path)
    {
        var registry = BuildRegistry();
        var loader = registry.GetLoader(path);
        Assert.IsType<PlainTextLoader>(loader);
    }

    [Theory]
    [InlineData("doc.md")]
    [InlineData("doc.markdown")]
    public void GetLoader_MarkdownFile_ReturnsMarkdownLoader(string path)
    {
        var registry = BuildRegistry();
        var loader = registry.GetLoader(path);
        Assert.IsType<MarkdownLoader>(loader);
    }

    [Theory]
    [InlineData("page.html")]
    [InlineData("page.htm")]
    public void GetLoader_HtmlFile_ReturnsHtmlLoader(string path)
    {
        var registry = BuildRegistry();
        var loader = registry.GetLoader(path);
        Assert.IsType<HtmlLoader>(loader);
    }

    [Fact]
    public void GetLoader_UnknownExtension_Throws()
    {
        var registry = BuildRegistry();
        Assert.Throws<NotSupportedException>(() => registry.GetLoader("archive.zip"));
    }
}
