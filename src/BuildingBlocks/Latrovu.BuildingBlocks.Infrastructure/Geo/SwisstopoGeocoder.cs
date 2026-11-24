using System.Diagnostics;
using System.Text.Json.Nodes;
using Latrovu.BuildingBlocks.Geo;

namespace Latrovu.BuildingBlocks.Infrastructure.Geo;

public sealed class SwisstopoGeocoder(HttpClient http, ILogger<SwisstopoGeocoder> logger) : IGeocoder
{
    public const string BaseUrl = "https://api3.geo.admin.ch/rest/services/api/";
    private const string AddressOrigin = "address";
    private const string PostalCodeOrigin = "zipcode";
    private const string QueryFormat = "SearchServer?type=locations&limit=1&sr=2056&origins={0}&searchText={1}";

    public async Task<GeoPoint?> GeocodeAsync(PostalAddress address, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(address);

        Stopwatch stopwatch = Stopwatch.StartNew();
        GeoPoint? point = await SearchAsync(AddressOrigin, address.FullText, GeoPoint.AddressPrecision, ct)
            ?? await SearchAsync(PostalCodeOrigin, address.PostalCode, GeoPoint.PostalCodePrecision, ct);
        logger.LogInformation(
            "Geocoding finished with precision {Precision} in {ElapsedMs} ms",
            point?.Precision ?? "none",
            stopwatch.ElapsedMilliseconds);
        return point;
    }

    private async Task<GeoPoint?> SearchAsync(string origin, string? text, string precision, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        string query = string.Format(System.Globalization.CultureInfo.InvariantCulture, QueryFormat, origin, Uri.EscapeDataString(text));
        using HttpResponseMessage response = await http.GetAsync(query, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Geocoding request failed with {StatusCode}", (int)response.StatusCode);
            return null;
        }

        JsonNode? attributes = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))?["results"]?[0]?["attrs"];
        double? x = attributes?["x"]?.GetValue<double>();
        double? y = attributes?["y"]?.GetValue<double>();
        return x is null || y is null ? null : new GeoPoint(Math.Max(x.Value, y.Value), Math.Min(x.Value, y.Value), precision);
    }
}
