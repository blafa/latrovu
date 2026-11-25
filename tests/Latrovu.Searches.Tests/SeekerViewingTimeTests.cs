namespace Latrovu.Searches.Tests;

public sealed class SeekerViewingTimeTests
{
    private static readonly DateTimeOffset StartsAt = new(2026, 12, 5, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndsAt = StartsAt.AddMinutes(30);

    [Theory]
    [InlineData("de", "Dezember")]
    [InlineData("fr", "décembre")]
    [InlineData("it", "dicembre")]
    [InlineData("es", "December")]
    public void Describe_UsesLanguageWithEnglishFallback(string language, string month)
    {
        string described = SeekerViewingTime.Describe(StartsAt, EndsAt, language);

        Assert.Contains(month, described, StringComparison.Ordinal);
    }

    [Fact]
    public void Describe_ShowsSwissLocalTime()
    {
        string described = SeekerViewingTime.Describe(StartsAt, EndsAt, "de");

        Assert.EndsWith("14:00–14:30", described, StringComparison.Ordinal);
    }
}
