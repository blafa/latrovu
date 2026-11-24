namespace Latrovu.Searches;

public sealed record SeekerReply(
    string Address,
    string Channel,
    string Language,
    string? Summary,
    IReadOnlyList<string> Refinements,
    IReadOnlyList<Search> OpenSearches,
    bool Understood,
    PlaceResolution Places);
