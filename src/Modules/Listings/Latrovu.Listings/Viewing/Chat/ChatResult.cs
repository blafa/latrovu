namespace Latrovu.Listings.Viewing.Chat;

public sealed record ChatResult(ChatOutcome Outcome, IReadOnlyList<ChatMessage> Messages);
