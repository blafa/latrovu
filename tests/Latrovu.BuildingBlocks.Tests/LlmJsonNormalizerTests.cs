using System.Text.Json;
using Latrovu.BuildingBlocks.Ai;
using Xunit;

namespace Latrovu.BuildingBlocks.Tests;

public sealed class LlmJsonNormalizerTests
{
    private const string PlainJson = "{\"rooms\":3.5}";
    private const string PlainJsonWithRooms = "{\"rooms\":2.5}";

    [Fact]
    public void Normalize_PlainJson_ReturnsUnchanged()
    {
        string? actual = LlmJsonNormalizer.Normalize(PlainJson);

        Assert.Equal(PlainJson, actual);
    }

    [Fact]
    public void Normalize_FencedJson_StripsFence()
    {
        string fenced = "```json\n" + PlainJson + "\n```";

        string? actual = LlmJsonNormalizer.Normalize(fenced);

        Assert.Equal(PlainJson, actual);
    }

    [Fact]
    public void Normalize_InvalidJson_KeepsRawAnswer()
    {
        const string answer = "Die Wohnung hat 3.5 Zimmer";

        string? actual = LlmJsonNormalizer.Normalize(answer);

        using JsonDocument document = JsonDocument.Parse(actual!);
        Assert.Equal(answer, document.RootElement.GetProperty(LlmJsonNormalizer.RawAnswerProperty).GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_NullOrWhitespace_ReturnsNull(string? answer)
    {
        string? actual = LlmJsonNormalizer.Normalize(answer);

        Assert.Null(actual);
    }

    [Fact]
    public void Normalize_DuplicateProperties_KeepsFirst()
    {
        const string answer = "{\"sale\":{\"priceChf\":422000,\"priceChf\":1}}";

        string? actual = LlmJsonNormalizer.Normalize(answer);

        Assert.Equal("{\"sale\":{\"priceChf\":422000}}", actual);
    }

    [Fact]
    public void Normalize_PropertyNameWithQuote_IsDropped()
    {
        const string answer = "{\"rooms\":2.5,\"pricePerM2Chf\\\": null, \":null}";

        string? actual = LlmJsonNormalizer.Normalize(answer);

        Assert.Equal(PlainJsonWithRooms, actual);
    }
}
