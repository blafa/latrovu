using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Extraction.Units;

public static class UnitTableParser
{
    private const string RentedMarker = "vermietet";
    private const string ReservedMarker = "reserviert";

    public static IReadOnlyList<ListingUnit> Parse(string? json) => Parse(json, null);

    public static IReadOnlyList<ListingUnit> Parse(string? json, IReadOnlyList<string>? rows) =>
        UnitsOf(json)
            .Select(item => (Unit: ToUnit(item), Row: RowOf(item, rows)))
            .Where(pair => pair.Unit is not null)
            .Select(pair => rows is null ? pair.Unit! : VerifyAgainst(pair.Unit!, pair.Row ?? string.Empty))
            .Where(HasSubstance)
            .Select(Complete)
            .DistinctBy(unit => unit.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static ListingUnit VerifyAgainst(ListingUnit unit, string row)
    {
        ArgumentNullException.ThrowIfNull(unit);

        HashSet<decimal> numbers = [.. FrancAmounts.NumbersIn(row)];
        return unit with
        {
            LivingAreaM2 = KeepIfIn(unit.LivingAreaM2, numbers),
            NetRentChf = KeepIfIn(unit.NetRentChf, numbers),
            ChargesChf = KeepIfIn(unit.ChargesChf, numbers),
            GrossRentChf = KeepIfIn(unit.GrossRentChf, numbers),
            PriceChf = KeepIfIn(unit.PriceChf, numbers),
            Status = StatusIn(row)
        };
    }

    public static ListingUnit Complete(ListingUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        decimal? net = unit.NetRentChf ?? Positive(unit.GrossRentChf - unit.ChargesChf);
        decimal? charges = unit.ChargesChf ?? Positive(unit.GrossRentChf - unit.NetRentChf);
        decimal? gross = unit.GrossRentChf ?? net + charges;
        return unit with { NetRentChf = net, ChargesChf = charges, GrossRentChf = gross };
    }

    private static IEnumerable<JsonObject> UnitsOf(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return ((JsonNode.Parse(json) as JsonObject)?["units"] as JsonArray ?? []).OfType<JsonObject>();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? RowOf(JsonObject item, IReadOnlyList<string>? rows)
    {
        decimal? number = NumberOf(item["row"]);
        int index = number is null ? -1 : (int)number.Value - 1;
        return rows is not null && index >= 0 && index < rows.Count ? rows[index] : null;
    }

    private static ListingUnit? ToUnit(JsonObject unit)
    {
        string label = TextOf(unit["label"])?.Trim() ?? string.Empty;
        if (label.Length == 0)
        {
            return null;
        }

        decimal? floor = NumberOf(unit["floor"]);
        return new ListingUnit(
            label,
            NumberOf(unit["rooms"]),
            floor is null ? null : (int)Math.Round(floor.Value),
            NumberOf(unit["livingAreaM2"]),
            NumberOf(unit["netRentChf"]),
            NumberOf(unit["chargesChf"]),
            NumberOf(unit["grossRentChf"]),
            NumberOf(unit["priceChf"]),
            TextOf(unit["availableFrom"]),
            StatusOf(TextOf(unit["status"])));
    }

    private static UnitStatus StatusIn(string row)
    {
        if (string.IsNullOrWhiteSpace(row))
        {
            return UnitStatus.Unknown;
        }

        if (row.Contains(RentedMarker, StringComparison.OrdinalIgnoreCase))
        {
            return UnitStatus.Rented;
        }

        return row.Contains(ReservedMarker, StringComparison.OrdinalIgnoreCase) ? UnitStatus.Reserved : UnitStatus.Available;
    }

    private static decimal? KeepIfIn(decimal? value, HashSet<decimal> numbers) =>
        value is { } number && numbers.Contains(number) ? value : null;

    private static decimal? Positive(decimal? amount) => amount > 0 ? amount : null;

    private static bool HasSubstance(ListingUnit unit) =>
        unit.Rooms is not null || unit.LivingAreaM2 is not null || unit.NetRentChf is not null || unit.GrossRentChf is not null || unit.PriceChf is not null;

    private static UnitStatus StatusOf(string? status) => status switch
    {
        "available" => UnitStatus.Available,
        "reserved" => UnitStatus.Reserved,
        "rented" => UnitStatus.Rented,
        _ => UnitStatus.Unknown
    };

    private static decimal? NumberOf(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue(out decimal number) ? number : null;

    private static string? TextOf(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue(out string? text) && !string.IsNullOrWhiteSpace(text) ? text : null;
}
