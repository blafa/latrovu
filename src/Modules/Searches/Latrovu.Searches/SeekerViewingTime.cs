using System.Globalization;

namespace Latrovu.Searches;

public static class SeekerViewingTime
{
    private const string FallbackCulture = "en-GB";
    private const string LongDateFormat = "D";
    private const string TimeFormat = "HH:mm";

    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

    private static readonly Dictionary<string, string> CulturesByLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] = "de-CH",
        ["fr"] = "fr-CH",
        ["it"] = "it-CH",
        ["en"] = FallbackCulture
    };

    public static string Describe(DateTimeOffset startsAt, DateTimeOffset endsAt, string language)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(CulturesByLanguage.GetValueOrDefault(language ?? string.Empty, FallbackCulture));
        DateTimeOffset start = TimeZoneInfo.ConvertTime(startsAt, Zone);
        DateTimeOffset end = TimeZoneInfo.ConvertTime(endsAt, Zone);
        return $"{start.ToString(LongDateFormat, culture)}, {Clock(start)}–{Clock(end)}";
    }

    private static string Clock(DateTimeOffset moment) => moment.ToString(TimeFormat, CultureInfo.InvariantCulture);
}
