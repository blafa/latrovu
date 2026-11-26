using System.Globalization;
using System.Text.RegularExpressions;

namespace Latrovu.Listings.Extraction.Units;

public static partial class FrancAmounts
{
    public static IReadOnlyList<decimal> In(string line) =>
        FrancAmount().Matches(line ?? string.Empty)
            .Select(match => decimal.Parse(match.Value.Replace("'", string.Empty).Replace("’", string.Empty), CultureInfo.InvariantCulture))
            .ToList();

    public static IReadOnlyList<decimal> NumbersIn(string line) =>
        AnyNumber().Matches(line ?? string.Empty)
            .Select(match => decimal.Parse(match.Value.Replace("'", string.Empty).Replace("’", string.Empty), CultureInfo.InvariantCulture))
            .ToList();

    [GeneratedRegex(@"\b\d{1,3}(?:['’]\d{3})+(?:\.\d{2})?\b|\b\d{3,5}\.\d{2}\b")]
    private static partial Regex FrancAmount();

    [GeneratedRegex(@"\d{1,3}(?:['’]\d{3})+(?:\.\d+)?|\d+(?:\.\d+)?")]
    private static partial Regex AnyNumber();
}
