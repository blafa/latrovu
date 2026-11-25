namespace Latrovu.Core.Listings;

public sealed record AgentAnswered(
    Guid SearchId,
    Guid ListingId,
    string ListingTitle,
    IReadOnlyList<string> Questions,
    string Answer,
    string Reference = "");
