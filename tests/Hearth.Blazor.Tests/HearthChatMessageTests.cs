namespace Hearth.Blazor.Tests;

public sealed class HearthChatMessageTests : TestContext
{
    [Fact]
    public void Renders_Markdown_Bold_As_Strong()
    {
        var entry = new ChatEntry { Id = "1", Role = ChatRole.Assistant, Content = "**bold**" };
        var cut = RenderComponent<HearthChatMessage>(p => p
            .Add(x => x.Entry, entry)
            .Add(x => x.EnableMarkdown, true));

        Assert.Contains("<strong>bold</strong>", cut.Markup);
    }

    [Fact]
    public void Renders_Plaintext_Without_Html_When_EnableMarkdown_False()
    {
        var entry = new ChatEntry { Id = "1", Role = ChatRole.Assistant, Content = "**bold**" };
        var cut = RenderComponent<HearthChatMessage>(p => p
            .Add(x => x.Entry, entry)
            .Add(x => x.EnableMarkdown, false));

        Assert.Contains("**bold**", cut.Markup);
        Assert.DoesNotContain("<strong>", cut.Markup);
    }

    [Fact]
    public void Shows_Streaming_Cursor_When_IsStreaming()
    {
        var entry = new ChatEntry { Id = "1", Role = ChatRole.Assistant, Content = "typing", IsStreaming = true };
        var cut = RenderComponent<HearthChatMessage>(p => p.Add(x => x.Entry, entry));

        Assert.Contains("hearth-cursor", cut.Markup);
    }

    [Fact]
    public void Does_Not_Show_Cursor_When_Not_Streaming()
    {
        var entry = new ChatEntry { Id = "1", Role = ChatRole.Assistant, Content = "done" };
        var cut = RenderComponent<HearthChatMessage>(p => p.Add(x => x.Entry, entry));

        Assert.DoesNotContain("hearth-cursor", cut.Markup);
    }

    [Fact]
    public void Applies_User_Role_Css_Class()
    {
        var entry = new ChatEntry { Id = "1", Role = ChatRole.User, Content = "Hello" };
        var cut = RenderComponent<HearthChatMessage>(p => p.Add(x => x.Entry, entry));

        Assert.Contains("hearth-message-user", cut.Markup);
    }

    [Fact]
    public void Applies_Assistant_Role_Css_Class()
    {
        var entry = new ChatEntry { Id = "1", Role = ChatRole.Assistant, Content = "Hi there" };
        var cut = RenderComponent<HearthChatMessage>(p => p.Add(x => x.Entry, entry));

        Assert.Contains("hearth-message-assistant", cut.Markup);
    }
}
