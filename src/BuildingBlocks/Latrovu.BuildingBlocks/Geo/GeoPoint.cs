namespace Latrovu.BuildingBlocks.Geo;

public sealed record GeoPoint(double East, double North, string Precision)
{
    public const string AddressPrecision = "address";
    public const string PostalCodePrecision = "zip";
}
