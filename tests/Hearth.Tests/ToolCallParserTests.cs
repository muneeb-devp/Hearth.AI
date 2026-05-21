namespace Hearth.Tests;

public sealed class ToolCallParserTests
{
    [Fact]
    public void TryParse_ValidToolCall_ReturnsTrue()
    {
        const string json = """{"tool_call": {"name": "get_weather", "arguments": {"location": "London"}}}""";

        var parsed = ToolCallParser.TryParse(json, out var name, out var args);

        Assert.True(parsed);
        Assert.Equal("get_weather", name);
        Assert.NotNull(args);
        Assert.True(args.ContainsKey("location"));
    }

    [Fact]
    public void TryParse_NoArguments_ReturnsTrueWithEmptyDict()
    {
        const string json = """{"tool_call": {"name": "ping", "arguments": {}}}""";

        var parsed = ToolCallParser.TryParse(json, out var name, out var args);

        Assert.True(parsed);
        Assert.Equal("ping", name);
        Assert.NotNull(args);
        Assert.Empty(args);
    }

    [Fact]
    public void TryParse_MissingArguments_ReturnsTrueWithEmptyDict()
    {
        const string json = """{"tool_call": {"name": "ping"}}""";

        var parsed = ToolCallParser.TryParse(json, out var name, out var args);

        Assert.True(parsed);
        Assert.Equal("ping", name);
        Assert.NotNull(args);
        Assert.Empty(args);
    }

    [Fact]
    public void TryParse_WrappedInCodeBlock_ExtractsJson()
    {
        var text = "```json\n{\"tool_call\": {\"name\": \"search\", \"arguments\": {\"query\": \"cats\"}}}\n```";

        var parsed = ToolCallParser.TryParse(text, out var name, out _);

        Assert.True(parsed);
        Assert.Equal("search", name);
    }

    [Fact]
    public void TryParse_ExtraTextBeforeJson_ExtractsJson()
    {
        var text = "I'll use a tool:\n{\"tool_call\": {\"name\": \"calc\", \"arguments\": {\"x\": 1}}}";

        var parsed = ToolCallParser.TryParse(text, out var name, out _);

        Assert.True(parsed);
        Assert.Equal("calc", name);
    }

    [Fact]
    public void TryParse_PlainText_ReturnsFalse()
    {
        var parsed = ToolCallParser.TryParse("The answer is 42.", out _, out _);
        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        var parsed = ToolCallParser.TryParse(string.Empty, out _, out _);
        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_JsonWithoutToolCall_ReturnsFalse()
    {
        const string json = """{"name": "foo", "value": 123}""";

        var parsed = ToolCallParser.TryParse(json, out _, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_MalformedJson_ReturnsFalse()
    {
        var parsed = ToolCallParser.TryParse("{tool_call: {bad json", out _, out _);
        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_EmptyToolName_ReturnsFalse()
    {
        const string json = """{"tool_call": {"name": "", "arguments": {}}}""";

        var parsed = ToolCallParser.TryParse(json, out _, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_MultipleArguments_ParsesAll()
    {
        const string json = """{"tool_call": {"name": "create_event", "arguments": {"title": "Meeting", "day": "Monday", "duration": 60}}}""";

        var parsed = ToolCallParser.TryParse(json, out _, out var args);

        Assert.True(parsed);
        Assert.NotNull(args);
        Assert.Equal(3, args.Count);
        Assert.True(args.ContainsKey("title"));
        Assert.True(args.ContainsKey("day"));
        Assert.True(args.ContainsKey("duration"));
    }

    [Fact]
    public void TryParse_ArgumentLookup_IsCaseInsensitive()
    {
        const string json = """{"tool_call": {"name": "f", "arguments": {"MyParam": "val"}}}""";

        ToolCallParser.TryParse(json, out _, out var args);

        Assert.NotNull(args);
        Assert.True(args.ContainsKey("myparam"));
        Assert.True(args.ContainsKey("MYPARAM"));
    }
}
