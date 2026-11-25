namespace Latrovu.Listings.Viewing.Chat;

public sealed record AgentReplyTarget(
    Guid BatchId,
    Guid ListingId,
    string TokenHash,
    string AgentAddress,
    string Title,
    Guid? SearchId,
    IReadOnlyList<string> Questions,
    string ListingDataJson = "{}",
    string? SeekerAddress = null);
