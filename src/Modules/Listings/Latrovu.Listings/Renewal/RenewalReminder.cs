namespace Latrovu.Listings.Renewal;

public sealed record RenewalReminder(
    string AgentAddress,
    string Title,
    int Number,
    string RenewToken,
    string ReviewToken,
    DateTimeOffset? DeletionAt);
