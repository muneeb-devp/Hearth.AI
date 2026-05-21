using Microsoft.Extensions.AI;

namespace Hearth.Tests;

public sealed class ChatTemplateTests
{
    [Fact]
    public void FormatChatML_EmptyList_ReturnsAssistantPromptOnly()
    {
        var result = ChatTemplate.FormatChatML([]);

        Assert.Equal("<|im_start|>assistant\n", result);
    }

    [Fact]
    public void FormatChatML_SingleUserMessage_HasCorrectStructure()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello!")
        };

        var result = ChatTemplate.FormatChatML(messages);

        Assert.Contains("<|im_start|>user\nHello!<|im_end|>", result);
        Assert.EndsWith("<|im_start|>assistant\n", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatChatML_SystemMessage_UsesSystemRole()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Be concise."),
            new(ChatRole.User, "Hi")
        };

        var result = ChatTemplate.FormatChatML(messages);

        Assert.StartsWith("<|im_start|>system\n", result, StringComparison.Ordinal);
        Assert.Contains("Be concise.<|im_end|>", result);
    }

    [Fact]
    public void FormatChatML_MultiTurn_PreservesChronologicalOrder()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "2+2?"),
            new(ChatRole.Assistant, "4"),
            new(ChatRole.User, "3+3?")
        };

        var result = ChatTemplate.FormatChatML(messages);

        var firstUserPos = result.IndexOf("<|im_start|>user", StringComparison.Ordinal);
        var assistantPos = result.IndexOf("<|im_start|>assistant\n4", StringComparison.Ordinal);
        var secondUserPos = result.IndexOf("<|im_start|>user", firstUserPos + 1, StringComparison.Ordinal);

        Assert.True(firstUserPos >= 0);
        Assert.True(firstUserPos < assistantPos);
        Assert.True(assistantPos < secondUserPos);
    }

    [Fact]
    public void FormatChatML_AlwaysEndsWithAssistantTurnOpening()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "anything")
        };

        var result = ChatTemplate.FormatChatML(messages);

        Assert.EndsWith("<|im_start|>assistant\n", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatChatML_NullMessageText_TreatedAsEmpty()
    {
        var message = new ChatMessage { Role = ChatRole.User };
        var result = ChatTemplate.FormatChatML([message]);

        Assert.Contains("<|im_start|>user\n<|im_end|>", result);
    }

    [Fact]
    public void FormatChatML_UnknownRole_UsesRoleValue()
    {
        var toolRole = new ChatRole("tool");
        var message = new ChatMessage { Role = toolRole, Contents = [new TextContent("result")] };
        var result = ChatTemplate.FormatChatML([message]);

        Assert.Contains("<|im_start|>tool\n", result);
    }
}
