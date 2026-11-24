namespace Latrovu.Searches;

public sealed record PlaceResolution(
    IReadOnlyList<PlaceRef> Places,
    IReadOnlyList<string> Unknown,
    IReadOnlyList<PlaceChoice> Ambiguous)
{
    public static readonly PlaceResolution Empty = new([], [], []);
}
