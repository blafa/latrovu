namespace Latrovu.Listings.Renewal;

public sealed record RenewalState(
    Guid ListingId,
    string AgentAddress,
    string Title,
    DateTimeOffset RenewedAt,
    int RemindersSent,
    DateTimeOffset? LastReminderAt);
