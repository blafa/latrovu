using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Extraction;

public static class EvidenceNeedles
{
    private const decimal MinMatchableNumber = 100;
    private const int MinTextLength = 3;
    private const int MaxTextLength = 120;
    private const string GroupedNumberFormat = "#,0";
    private const string DecimalNumberFormat = "0.0#";
    private const string PlainNumberFormat = "0";
    private static readonly string[] ThousandsSeparators = ["'", " ", "."];

    public static IReadOnlyList<string> FromValue(JsonNode value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.GetValueKind() switch
        {
            JsonValueKind.Number => NumberVariants(value.GetValue<decimal>()),
            JsonValueKind.String => TextVariants(value.GetValue<string>()),
            _ => []
        };
    }

    private static IReadOnlyList<string> TextVariants(string text)
    {
        string trimmed = text.Trim();
        return trimmed.Length is >= MinTextLength and <= MaxTextLength ? [trimmed] : [];
    }

    private static IReadOnlyList<string> NumberVariants(decimal number)
    {
        if (number < MinMatchableNumber)
        {
            return [];
        }

        if (number != decimal.Truncate(number))
        {
            string withPoint = number.ToString(DecimalNumberFormat, CultureInfo.InvariantCulture);
            return [withPoint, withPoint.Replace('.', ',')];
        }

        string grouped = number.ToString(GroupedNumberFormat, CultureInfo.InvariantCulture);
        return
        [
            .. ThousandsSeparators.Select(separator => grouped.Replace(",", separator, StringComparison.Ordinal)),
            number.ToString(PlainNumberFormat, CultureInfo.InvariantCulture)
        ];
    }
}
