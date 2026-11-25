using Latrovu.Listings.Viewing.Viewings;

namespace Latrovu.Listings.Tests.Viewing;

public sealed class SwissTimeTests
{
    [Fact]
    public void ToUtc_WinterTime_IsOneHourBehind()
    {
        Assert.Equal(new DateTimeOffset(2026, 12, 5, 9, 0, 0, TimeSpan.Zero), SwissTime.ToUtc("05.12.2026", "10:00"));
    }

    [Fact]
    public void ToUtc_SummerTime_IsTwoHoursBehind()
    {
        Assert.Equal(new DateTimeOffset(2026, 7, 5, 8, 0, 0, TimeSpan.Zero), SwissTime.ToUtc("2026-07-05", "10:00"));
    }

    [Theory]
    [InlineData("5.12.2026", "9:30")]
    [InlineData("05.12.2026", "09.30")]
    [InlineData(" 05.12.2026 ", " 9:30 ")]
    public void ToUtc_CommonSwissVariants_AreAccepted(string date, string time)
    {
        Assert.Equal(new DateTimeOffset(2026, 12, 5, 8, 30, 0, TimeSpan.Zero), SwissTime.ToUtc(date, time));
    }

    [Theory]
    [InlineData("10")]
    [InlineData("1000")]
    [InlineData("10.00")]
    public void ToUtc_ShortHourForms_AreAccepted(string time)
    {
        Assert.Equal(new DateTimeOffset(2026, 12, 5, 9, 0, 0, TimeSpan.Zero), SwissTime.ToUtc("05.12.2026", time));
    }

    [Fact]
    public void ToUtc_SingleDigitHour_IsAccepted()
    {
        Assert.Equal(new DateTimeOffset(2026, 12, 5, 8, 0, 0, TimeSpan.Zero), SwissTime.ToUtc("05.12.2026", "9"));
    }

    [Theory]
    [InlineData("32.12.2026", "10:00")]
    [InlineData("05.12.2026", "10 Uhr")]
    [InlineData("", "")]
    public void ToUtc_InvalidInput_ReturnsNull(string date, string time)
    {
        Assert.Null(SwissTime.ToUtc(date, time));
    }
}
