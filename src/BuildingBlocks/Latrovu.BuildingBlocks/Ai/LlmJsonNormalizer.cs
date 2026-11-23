using System.Text;
using System.Text.Json;

namespace Latrovu.BuildingBlocks.Ai;

public static class LlmJsonNormalizer
{
    public const string RawAnswerProperty = "_raw";
    private const string CodeFence = "```";
    private const char LineBreak = '\n';
    private const char Quote = '"';

    public static string? Normalize(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return null;
        }

        string candidate = StripCodeFence(answer.Trim());
        try
        {
            return RemoveInvalidAndDuplicateProperties(candidate);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new Dictionary<string, string> { [RawAnswerProperty] = answer });
        }
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

    private static string RemoveInvalidAndDuplicateProperties(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            WriteClean(writer, document.RootElement);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteClean(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteCleanObject(writer, element);
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    WriteClean(writer, item);
                }

                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static void WriteCleanObject(Utf8JsonWriter writer, JsonElement element)
    {
        writer.WriteStartObject();
        HashSet<string> seenNames = [];
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (IsValidPropertyName(property.Name) && seenNames.Add(property.Name))
            {
                writer.WritePropertyName(property.Name);
                WriteClean(writer, property.Value);
            }
        }

        writer.WriteEndObject();
    }

    private static bool IsValidPropertyName(string name) =>
        name.Length > 0 && !name.Contains(Quote) && !name.Any(char.IsControl);
}
