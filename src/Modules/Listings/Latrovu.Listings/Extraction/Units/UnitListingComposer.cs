using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Extraction.Units;

public static class UnitListingComposer
{
    private const string VerifiedScore = "high";
    private const decimal Half = 0.5m;
    private static readonly string[] BuildingWidePaths = ["referenceNumber", "size.usableAreaM2", "size.balconyAreaM2", "size.bedrooms"];
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public static ListingExtraction Compose(ListingExtraction building, ListingUnit unit)
    {
        ArgumentNullException.ThrowIfNull(building);
        ArgumentNullException.ThrowIfNull(unit);

        JsonObject data = JsonNode.Parse(building.DataJson) as JsonObject ?? [];
        Dictionary<string, string> scores = JsonSerializer.Deserialize<Dictionary<string, string>>(building.FieldScoresJson) ?? [];
        Dictionary<string, EvidenceLocation> evidence = JsonSerializer.Deserialize<Dictionary<string, EvidenceLocation>>(building.FieldEvidenceJson, WebJson) ?? [];
        foreach (string path in BuildingWidePaths)
        {
            Write(data, scores, evidence, path, null);
        }

        foreach ((string path, JsonNode? value) in UnitValues(unit))
        {
            Write(data, scores, evidence, path, value);
        }

        return new ListingExtraction(building.ExtractedJson, data.ToJsonString(), JsonSerializer.Serialize(scores), JsonSerializer.Serialize(evidence, WebJson));
    }

    public static string Title(ListingUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        return unit.Rooms is { } rooms ? $"{RoomsText(rooms)}-Zimmer-Wohnung, {unit.Label}" : $"Wohnung, {unit.Label}";
    }

    private static IEnumerable<(string Path, JsonNode? Value)> UnitValues(ListingUnit unit)
    {
        yield return ("title", JsonValue.Create(Title(unit)));
        yield return ("size.rooms", Number(unit.Rooms));
        yield return ("size.livingAreaM2", Number(unit.LivingAreaM2));
        yield return ("building.floor", unit.Floor is { } floor ? JsonValue.Create(floor) : null);
        yield return ("rent.netChf", Number(unit.NetRentChf));
        yield return ("rent.chargesChf", Number(unit.ChargesChf));
        yield return ("rent.grossChf", Number(unit.GrossRentChf));
        if (unit.PriceChf is not null)
        {
            yield return ("sale.priceChf", Number(unit.PriceChf));
        }

        if (unit.AvailableFrom is not null)
        {
            yield return ("availability.availableFrom", JsonValue.Create(unit.AvailableFrom));
        }
    }

    private static void Write(JsonObject data, Dictionary<string, string> scores, Dictionary<string, EvidenceLocation> evidence, string path, JsonNode? value)
    {
        JsonPaths.Set(data, path, value);
        evidence.Remove(path);
        if (value is null)
        {
            scores.Remove(path);
        }
        else
        {
            scores[path] = VerifiedScore;
        }
    }

    private static JsonNode? Number(decimal? value) => value is { } number ? JsonValue.Create(number) : null;

    public static string RoomsText(decimal rooms) =>
        rooms % 1 == Half
            ? $"{Math.Floor(rooms).ToString(CultureInfo.InvariantCulture)}½"
            : rooms.ToString("0.#", CultureInfo.InvariantCulture);
}
