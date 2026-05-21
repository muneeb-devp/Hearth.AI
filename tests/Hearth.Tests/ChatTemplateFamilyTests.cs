using Microsoft.Extensions.AI;

namespace Hearth.Tests;

public sealed class ChatTemplateFamilyTests
{
    // --- DetectFamily ---

    [Theory]
    [InlineData("meta-llama-3-8b-instruct-q4_k_m.gguf")]
    [InlineData("Llama-3.2-3B-Instruct-Q4_K_M.gguf")]
    [InlineData("llama3-8b-q5_k_m.gguf")]
    public void DetectFamily_Llama3Filenames_ReturnLlama3(string filename)
    {
        Assert.Equal(ChatTemplateFamily.Llama3, ChatTemplate.DetectFamily(filename));
    }

    [Theory]
    [InlineData("gemma-2-9b-it-q4_k_m.gguf")]
    [InlineData("gemma-7b-q4_k_m.gguf")]
    public void DetectFamily_GemmaFilenames_ReturnGemma(string filename)
    {
        Assert.Equal(ChatTemplateFamily.Gemma, ChatTemplate.DetectFamily(filename));
    }

    [Theory]
    [InlineData("phi-3-mini-4k-instruct-q4.gguf")]
    [InlineData("phi3-medium-q5_k_m.gguf")]
    [InlineData("phi-4-q4_k_m.gguf")]
    public void DetectFamily_Phi3Filenames_ReturnPhi3(string filename)
    {
        Assert.Equal(ChatTemplateFamily.Phi3, ChatTemplate.DetectFamily(filename));
    }

    [Theory]
    [InlineData("qwen2.5-7b-instruct-q4_k_m.gguf")]
    [InlineData("mistral-7b-instruct-q4_k_m.gguf")]
    [InlineData("unknown-model-q4_k_m.gguf")]
    public void DetectFamily_DefaultFilenames_ReturnChatML(string filename)
    {
        Assert.Equal(ChatTemplateFamily.ChatML, ChatTemplate.DetectFamily(filename));
    }

    [Fact]
    public void DetectFamily_UsesFilenameOnly_NotFullPath()
    {
        var result = ChatTemplate.DetectFamily("/models/llama3-8b.gguf");
        Assert.Equal(ChatTemplateFamily.Llama3, result);
    }

    // --- FormatLlama3 ---

    [Fact]
    public void FormatLlama3_StartsWithBeginOfText()
    {
        var result = ChatTemplate.FormatLlama3([]);
        Assert.StartsWith("<|begin_of_text|>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLlama3_EndsWithAssistantHeader()
    {
        var result = ChatTemplate.FormatLlama3([new(ChatRole.User, "hi")]);
        Assert.EndsWith("<|start_header_id|>assistant<|end_header_id|>\n\n", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLlama3_SystemMessage_UsesSystemHeader()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Be helpful."),
            new(ChatRole.User, "Hello"),
        };

        var result = ChatTemplate.FormatLlama3(messages);

        Assert.Contains("<|start_header_id|>system<|end_header_id|>\n\nBe helpful.<|eot_id|>", result, StringComparison.Ordinal);
        Assert.Contains("<|start_header_id|>user<|end_header_id|>\n\nHello<|eot_id|>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLlama3_MultiTurn_ContainsEotIdTokens()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "What is 2+2?"),
            new(ChatRole.Assistant, "4"),
        };

        var result = ChatTemplate.FormatLlama3(messages);

        Assert.Equal(2, result.Split("<|eot_id|>").Length - 1);
    }

    // --- FormatGemma ---

    [Fact]
    public void FormatGemma_EndsWithModelTurnOpening()
    {
        var result = ChatTemplate.FormatGemma([new(ChatRole.User, "hi")]);
        Assert.EndsWith("<start_of_turn>model\n", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatGemma_SystemMessage_PrependedToFirstUser()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Be concise."),
            new(ChatRole.User, "Hello"),
        };

        var result = ChatTemplate.FormatGemma(messages);

        var userTurnStart = result.IndexOf("<start_of_turn>user\n", StringComparison.Ordinal);
        Assert.True(userTurnStart >= 0);

        var afterUserTurn = result[(userTurnStart + "<start_of_turn>user\n".Length)..];
        Assert.StartsWith("Be concise.", afterUserTurn, StringComparison.Ordinal);
        Assert.Contains("Hello", afterUserTurn, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatGemma_AssistantRole_UsesModelTurn()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hi"),
            new(ChatRole.Assistant, "Hey"),
        };

        var result = ChatTemplate.FormatGemma(messages);

        Assert.Contains("<start_of_turn>model\nHey<end_of_turn>", result, StringComparison.Ordinal);
    }

    // --- FormatPhi3 ---

    [Fact]
    public void FormatPhi3_EndsWithAssistantToken()
    {
        var result = ChatTemplate.FormatPhi3([new(ChatRole.User, "hi")]);
        Assert.EndsWith("<|assistant|>\n", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatPhi3_SystemMessage_UsesSystemToken()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "Be helpful."),
            new(ChatRole.User, "Hello"),
        };

        var result = ChatTemplate.FormatPhi3(messages);

        Assert.Contains("<|system|>\nBe helpful.<|end|>", result, StringComparison.Ordinal);
        Assert.Contains("<|user|>\nHello<|end|>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatPhi3_AssistantTurn_UsesAssistantToken()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hi"),
            new(ChatRole.Assistant, "Hey"),
        };

        var result = ChatTemplate.FormatPhi3(messages);

        Assert.Contains("<|assistant|>\nHey<|end|>", result, StringComparison.Ordinal);
    }

    // --- GetAntiPrompts ---

    [Fact]
    public void GetAntiPrompts_Llama3_ContainsEotId()
    {
        var prompts = ChatTemplate.GetAntiPrompts(ChatTemplateFamily.Llama3);
        Assert.Contains("<|eot_id|>", prompts);
    }

    [Fact]
    public void GetAntiPrompts_Gemma_ContainsEndOfTurn()
    {
        var prompts = ChatTemplate.GetAntiPrompts(ChatTemplateFamily.Gemma);
        Assert.Contains("<end_of_turn>", prompts);
    }

    [Fact]
    public void GetAntiPrompts_Phi3_ContainsEndToken()
    {
        var prompts = ChatTemplate.GetAntiPrompts(ChatTemplateFamily.Phi3);
        Assert.Contains("<|end|>", prompts);
    }

    [Fact]
    public void GetAntiPrompts_ChatML_ContainsImEnd()
    {
        var prompts = ChatTemplate.GetAntiPrompts(ChatTemplateFamily.ChatML);
        Assert.Contains("<|im_end|>", prompts);
    }
}
