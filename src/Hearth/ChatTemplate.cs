using System.Text;
using Microsoft.Extensions.AI;

namespace Hearth;

internal static class ChatTemplate
{
    internal static readonly string[] AntiPromptsChatML = ["<|im_end|>", "</s>", "<|eot_id|>", "<|end|>"];
    internal static readonly string[] AntiPromptsLlama3 = ["<|eot_id|>", "<|end_of_text|>", "<|im_end|>"];
    internal static readonly string[] AntiPromptsGemma  = ["<end_of_turn>", "</s>"];
    internal static readonly string[] AntiPromptsPhi3   = ["<|end|>", "<|endoftext|>", "</s>"];

    internal static ChatTemplateFamily DetectFamily(string modelPath)
    {
        var name = Path.GetFileNameWithoutExtension(modelPath).ToLowerInvariant();

        if (name.Contains("llama-3", StringComparison.Ordinal)
            || name.Contains("llama3", StringComparison.Ordinal)
            || name.Contains("meta-llama-3", StringComparison.Ordinal))
        {
            return ChatTemplateFamily.Llama3;
        }

        if (name.Contains("gemma", StringComparison.Ordinal))
        {
            return ChatTemplateFamily.Gemma;
        }

        if (name.Contains("phi-3", StringComparison.Ordinal)
            || name.Contains("phi3", StringComparison.Ordinal)
            || name.Contains("phi-4", StringComparison.Ordinal)
            || name.Contains("phi4", StringComparison.Ordinal))
        {
            return ChatTemplateFamily.Phi3;
        }

        return ChatTemplateFamily.ChatML;
    }

    internal static string[] GetAntiPrompts(ChatTemplateFamily family) => family switch
    {
        ChatTemplateFamily.Llama3 => AntiPromptsLlama3,
        ChatTemplateFamily.Gemma  => AntiPromptsGemma,
        ChatTemplateFamily.Phi3   => AntiPromptsPhi3,
        _                         => AntiPromptsChatML,
    };

    internal static string Format(IEnumerable<ChatMessage> messages, ChatTemplateFamily family) => family switch
    {
        ChatTemplateFamily.Llama3 => FormatLlama3(messages),
        ChatTemplateFamily.Gemma  => FormatGemma(messages),
        ChatTemplateFamily.Phi3   => FormatPhi3(messages),
        _                         => FormatChatML(messages),
    };

    internal static string FormatChatML(IEnumerable<ChatMessage> messages)
    {
        var sb = new StringBuilder(capacity: 512);

        foreach (var message in messages)
        {
            sb.Append("<|im_start|>");
            sb.Append(GetChatMLRole(message.Role));
            sb.Append('\n');
            sb.Append(message.Text ?? string.Empty);
            sb.Append("<|im_end|>\n");
        }

        sb.Append("<|im_start|>assistant\n");
        return sb.ToString();
    }

    internal static string FormatLlama3(IEnumerable<ChatMessage> messages)
    {
        var sb = new StringBuilder(capacity: 512);
        sb.Append("<|begin_of_text|>");

        foreach (var message in messages)
        {
            sb.Append("<|start_header_id|>");
            sb.Append(GetLlama3Role(message.Role));
            sb.Append("<|end_header_id|>\n\n");
            sb.Append(message.Text ?? string.Empty);
            sb.Append("<|eot_id|>");
        }

        sb.Append("<|start_header_id|>assistant<|end_header_id|>\n\n");
        return sb.ToString();
    }

    internal static string FormatGemma(IEnumerable<ChatMessage> messages)
    {
        var sb = new StringBuilder(capacity: 512);
        string? pendingSystem = null;
        var firstUserSeen = false;

        foreach (var message in messages)
        {
            if (message.Role == ChatRole.System)
            {
                pendingSystem = message.Text ?? string.Empty;
                continue;
            }

            if (message.Role == ChatRole.User)
            {
                sb.Append("<start_of_turn>user\n");

                if (!firstUserSeen && pendingSystem is not null)
                {
                    sb.Append(pendingSystem);
                    sb.Append('\n');
                    pendingSystem = null;
                }

                firstUserSeen = true;
                sb.Append(message.Text ?? string.Empty);
                sb.Append("<end_of_turn>\n");
            }
            else if (message.Role == ChatRole.Assistant)
            {
                sb.Append("<start_of_turn>model\n");
                sb.Append(message.Text ?? string.Empty);
                sb.Append("<end_of_turn>\n");
            }
            else
            {
                sb.Append("<start_of_turn>");
                sb.Append(message.Role.Value);
                sb.Append('\n');
                sb.Append(message.Text ?? string.Empty);
                sb.Append("<end_of_turn>\n");
            }
        }

        sb.Append("<start_of_turn>model\n");
        return sb.ToString();
    }

    internal static string FormatPhi3(IEnumerable<ChatMessage> messages)
    {
        var sb = new StringBuilder(capacity: 512);

        foreach (var message in messages)
        {
            sb.Append(GetPhi3RoleToken(message.Role));
            sb.Append('\n');
            sb.Append(message.Text ?? string.Empty);
            sb.Append("<|end|>\n");
        }

        sb.Append("<|assistant|>\n");
        return sb.ToString();
    }

    private static string GetChatMLRole(ChatRole role)
    {
        if (role == ChatRole.System)
        {
            return "system";
        }

        if (role == ChatRole.User)
        {
            return "user";
        }

        if (role == ChatRole.Assistant)
        {
            return "assistant";
        }

        return role.Value;
    }

    private static string GetLlama3Role(ChatRole role)
    {
        if (role == ChatRole.System)
        {
            return "system";
        }

        if (role == ChatRole.User)
        {
            return "user";
        }

        if (role == ChatRole.Assistant)
        {
            return "assistant";
        }

        return role.Value;
    }

    private static string GetPhi3RoleToken(ChatRole role)
    {
        if (role == ChatRole.System)
        {
            return "<|system|>";
        }

        if (role == ChatRole.User)
        {
            return "<|user|>";
        }

        if (role == ChatRole.Assistant)
        {
            return "<|assistant|>";
        }

        return $"<|{role.Value}|>";
    }
}
