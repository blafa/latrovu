namespace Latrovu.Searches;

public sealed record PlaceExpression(string Name, string? Within, string? Kind, double? NearKm);
