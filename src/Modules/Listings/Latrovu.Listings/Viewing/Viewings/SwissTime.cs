using System.Globalization;

namespace Latrovu.Listings.Viewing.Viewings;

public static class SwissTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");
    private static readonly CultureInfo SwissCulture = CultureInfo.GetCultureInfo("de-CH");

    private static readonly string[] DateFormats = ["d.M.yyyy", "dd.MM.yyyy", "yyyy-MM-dd"];
    private static readonly string[] TimeFormats = ["H:mm", "HH:mm", "H.mm", "HH.mm", "%H", "HH", "Hmm", "HHmm"];

    public static DateTimeOffset? ToUtc(string date, string time)
    {
        if (!DateOnly.TryParseExact(date?.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day)
            || !TimeOnly.TryParseExact(time?.Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly clock))
        {
            return null;
        }

        DateTime local = day.ToDateTime(clock, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }

    public static DateTimeOffset ToLocal(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, Zone);

    public static string Describe(ViewingSlot slot) => $"{Day(slot)} · {Hours(slot)}";

    public static string Day(ViewingSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        return TimeZoneInfo.ConvertTime(slot.StartsAt, Zone).ToString("ddd d. MMM", SwissCulture);
    }

    public static string Hours(ViewingSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        return $"{TimeZoneInfo.ConvertTime(slot.StartsAt, Zone):HH:mm}–{TimeZoneInfo.ConvertTime(slot.EndsAt, Zone):HH:mm}";
    }
}
