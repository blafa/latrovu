using System.Text.Json;

namespace Latrovu.BuildingBlocks.Ai;

public static class LlmJsonNormalizer
{
    public const string RawAnswerProperty = "_raw";
    private const string CodeFence = "```";
    private const char LineBreak = '\n';

    public static string? Normalize(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return null;
        }

        string candidate = StripCodeFence(answer.Trim());
        if (IsValidJson(candidate))
        {
            return candidate;
        }

        return JsonSerializer.Serialize(new Dictionary<string, string> { [RawAnswerProperty] = answer });
    }

    private static string StripCodeFence(string text)
    {
        if (!text.StartsWith(CodeFence, StringComparison.Ordinal))
        {
            return text;
        }

        int contentStart = text.IndexOf(LineBreak) + 1;
        int contentEnd = text.LastIndexOf(CodeFence, StringComparison.Ordinal);
        return contentStart > 0 && contentEnd > contentStart
            ? text[contentStart..contentEnd].Trim()
            : text;
    }

    private static bool IsValidJson(string text)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
