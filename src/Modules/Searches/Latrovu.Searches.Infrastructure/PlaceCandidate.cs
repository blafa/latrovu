namespace Latrovu.Searches.Infrastructure;

internal sealed record PlaceCandidate(long Id, string Kind, string Name, string? Context, bool Exact, float Score);
