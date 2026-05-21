using System.Text.Json;

namespace Hearth;

internal static class ToolCallParser
{
    internal static bool TryParse(string response, out string? name, out IDictionary<string, object?>? arguments)
    {
        name = null;
        arguments = null;

        var json = ExtractJson(response);
        if (json is null)
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!root.TryGetProperty("tool_call", out var toolCall))
            {
                return false;
            }

            if (!toolCall.TryGetProperty("name", out var nameProp))
            {
                return false;
            }

            name = nameProp.GetString();
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (toolCall.TryGetProperty("arguments", out var argsProp) && argsProp.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in argsProp.EnumerateObject())
                {
                    args[prop.Name] = prop.Value.Clone();
                }
            }

            arguments = args;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ExtractJson(string text)
    {
        text = text.Trim();

        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0)
            {
                var closingFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (closingFence > firstNewline)
                {
                    text = text[(firstNewline + 1)..closingFence].Trim();
                }
            }
        }

        var start = text.IndexOf('{');
        if (start < 0)
        {
            return null;
        }

        var end = text.LastIndexOf('}');
        if (end <= start)
        {
            return null;
        }

        return text[start..(end + 1)];
    }
}
