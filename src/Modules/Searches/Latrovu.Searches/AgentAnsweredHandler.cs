using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public sealed class AgentAnsweredHandler(
    ISearchStore searches,
    ISeekerNotifier notifier,
    ILogger<AgentAnsweredHandler> logger) : IMessageHandler<AgentAnswered>
{
    public async Task HandleAsync(AgentAnswered message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        Search? search = await searches.FindByIdAsync(message.SearchId, ct);
        if (search is null)
        {
            logger.LogWarning("Agent answer for listing {ListingId} belongs to unknown search {SearchId}, no mail sent", message.ListingId, message.SearchId);
            return;
        }

        await notifier.SendAgentAnswerAsync(
            new SeekerAgentAnswer(search.SeekerAddress, search.Channel, search.Language, message.ListingTitle, message.Questions, message.Answer, message.Reference),
            ct);
        logger.LogInformation("Agent answer for listing {ListingId} sent to search {SearchId}", message.ListingId, message.SearchId);
    }
}
