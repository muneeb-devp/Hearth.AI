namespace Hearth.Rag.Tests.Documents;

public sealed class HtmlLoaderTests
{
    [Fact]
    public void ExtractText_StripsHtmlTags()
    {
        const string html = "<html><body><p>Hello <strong>world</strong></p></body></html>";
        var text = HtmlLoader.ExtractText(html);

        Assert.DoesNotContain("<", text);
        Assert.Contains("Hello", text);
        Assert.Contains("world", text);
    }

    [Fact]
    public void ExtractText_RemovesScriptContent()
    {
        const string html = "<html><body><script>alert('xss')</script><p>Safe text</p></body></html>";
        var text = HtmlLoader.ExtractText(html);

        Assert.DoesNotContain("alert", text);
        Assert.Contains("Safe text", text);
    }

    [Fact]
    public void ExtractText_RemovesStyleContent()
    {
        const string html = "<html><head><style>.foo { color: red; }</style></head><body><p>Content</p></body></html>";
        var text = HtmlLoader.ExtractText(html);

        Assert.DoesNotContain("color: red", text);
        Assert.Contains("Content", text);
    }

    [Fact]
    public void ExtractText_DecodesHtmlEntities()
    {
        const string html = "<p>Hello &amp; world &lt;3&gt;</p>";
        var text = HtmlLoader.ExtractText(html);

        Assert.Contains("Hello & world", text);
    }

    [Fact]
    public void CanLoad_HtmlExtensions_ReturnsTrue()
    {
        var loader = new HtmlLoader();
        Assert.True(loader.CanLoad("page.html"));
        Assert.True(loader.CanLoad("page.htm"));
        Assert.True(loader.CanLoad("PAGE.HTML"));
    }

    [Fact]
    public void CanLoad_NonHtmlExtension_ReturnsFalse()
    {
        var loader = new HtmlLoader();
        Assert.False(loader.CanLoad("file.txt"));
        Assert.False(loader.CanLoad("doc.md"));
    }
}
