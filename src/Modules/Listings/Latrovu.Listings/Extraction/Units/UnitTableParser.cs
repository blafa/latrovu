using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Extraction.Units;

public static class UnitTableParser
{
    public static IReadOnlyList<ListingUnit> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        JsonArray? units;
        try
        {
            units = (JsonNode.Parse(json) as JsonObject)?["units"] as JsonArray;
        }
        catch (JsonException)
        {
            return [];
        }

        return (units ?? [])
            .OfType<JsonObject>()
            .Select(ToUnit)
            .Where(unit => unit is not null && HasSubstance(unit))
            .Select(unit => Complete(unit!))
            .DistinctBy(unit => unit.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static ListingUnit Complete(ListingUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        decimal? net = unit.NetRentChf ?? Positive(unit.GrossRentChf - unit.ChargesChf);
        decimal? charges = unit.ChargesChf ?? Positive(unit.GrossRentChf - unit.NetRentChf);
        decimal? gross = unit.GrossRentChf ?? net + charges;
        return unit with { NetRentChf = net, ChargesChf = charges, GrossRentChf = gross };
    }

    private static decimal? Positive(decimal? amount) => amount > 0 ? amount : null;

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
