namespace Latrovu.BuildingBlocks.Geo;

public interface IGeocoder
{
    Task<GeoPoint?> GeocodeAsync(PostalAddress address, CancellationToken ct);
}
