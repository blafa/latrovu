namespace Latrovu.BuildingBlocks.Geo;

public sealed record PostalAddress(string? Street, string? HouseNumber, string? PostalCode, string? City)
{
    public string FullText => string.Join(' ', new[] { Street, HouseNumber, PostalCode, City }.Where(part => !string.IsNullOrWhiteSpace(part)));
}
