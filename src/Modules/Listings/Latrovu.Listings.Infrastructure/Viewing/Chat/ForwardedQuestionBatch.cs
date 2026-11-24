namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public sealed record ForwardedQuestionBatch(
    Guid BatchId,
    Guid ListingId,
    string AgentAddress,
    string Title,
    IReadOnlyList<long> MessageIds,
    IReadOnlyList<string> Questions)
{
    private const int ReferenceLength = 10;

    public string Reference => $"Q-{BatchId:N}"[..(ReferenceLength + 2)];
}
