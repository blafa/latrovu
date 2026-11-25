using Latrovu.Listings.Viewing.Viewings;

namespace Latrovu.Listings.Tests.Viewing;

public sealed class SlotChoiceTests
{
    private static readonly IReadOnlyList<ViewingSlot> Offered =
    [
        Slot(1, new DateTimeOffset(2026, 11, 27, 9, 0, 0, TimeSpan.Zero)),
        Slot(2, new DateTimeOffset(2026, 12, 10, 9, 0, 0, TimeSpan.Zero)),
        Slot(3, new DateTimeOffset(2026, 12, 12, 9, 0, 0, TimeSpan.Zero))
    ];

    [Theory]
    [InlineData("Ich nehme den Donnerstag", 2)]
    [InlineData("jeudi s'il vous plaît", 2)]
    [InlineData("giovedì", 2)]
    [InlineData("2", 2)]
    [InlineData("den zweiten bitte", 2)]
    [InlineData("am 12. passt mir", 3)]
    [InlineData("Freitag um 10", 1)]
    [InlineData("Thursday works for me", 2)]
    public void Match_ClearChoice_ReturnsThatSlot(string text, long expectedId)
    {
        Assert.Equal(expectedId, SlotChoice.Match(text, Offered)?.Id);
    }

    [Theory]
    [InlineData("um 10")]
    [InlineData("Haben Sie auch abends Termine?")]
    [InlineData("Yes I do")]
    [InlineData("Mittwoch")]
    [InlineData("10")]
    [InlineData("")]
    public void Match_UnclearOrNoChoice_ReturnsNull(string text)
    {
        Assert.Null(SlotChoice.Match(text, Offered));
    }

    private static ViewingSlot Slot(long id, DateTimeOffset startsAt) =>
        new(id, startsAt, startsAt.AddHours(1), 10, 0, false, []);
}
