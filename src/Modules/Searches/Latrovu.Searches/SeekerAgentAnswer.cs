namespace Latrovu.Searches;

public sealed record SeekerAgentAnswer(
    string Address,
    string Channel,
    string Language,
    string ListingTitle,
    IReadOnlyList<string> Questions,
    string Answer,
    string Reference = "");
