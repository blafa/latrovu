namespace Latrovu.Listings.Viewing.Chat;

public sealed record ChatMessage(
    string Role,
    string Text,
    string? Source,
    int? Page,
    DateTimeOffset CreatedAt,
    long Id = 0,
    string? ForwardStatus = null)
{
    public const string SeekerRole = "seeker";
    public const string AssistantRole = "assistant";
    public const string AgentRole = "agent";

    public const string ExposeSource = "expose";
    public const string DataSource = "data";
    public const string LatrovuSource = "latrovu";
    public const string RequestSource = "request";
    public const string UnknownSource = "unknown";

    public const string ForwardPending = "pending";
    public const string ForwardSent = "sent";

    public bool CanBeForwarded => Role == AssistantRole && Source == UnknownSource && ForwardStatus is null;
}
