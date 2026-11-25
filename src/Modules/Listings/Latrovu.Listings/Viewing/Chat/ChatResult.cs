using Latrovu.Listings.Viewing.Viewings;

namespace Latrovu.Listings.Viewing.Chat;

public sealed record ChatResult(ChatOutcome Outcome, IReadOnlyList<ChatMessage> Messages)
{
    public IReadOnlyList<ViewingSlot> Slots { get; init; } = [];
}
