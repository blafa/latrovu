using Latrovu.BuildingBlocks.Messaging;
using Latrovu.BuildingBlocks.Security;
using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public sealed class ListingAccessRequestedHandler(
    ISearchStore searches,
    ISeekerNotifier notifier,
    TimeProvider timeProvider,
    ILogger<ListingAccessRequestedHandler> logger) : IMessageHandler<ListingAccessRequested>
{
    public const string EmailChannel = "email";
    private const string EmptyCriteria = "{}";

    public async Task HandleAsync(ListingAccessRequested message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        Search search = await SearchForAsync(message, ct);
        string token = SecureTokens.Create();
        await searches.PublishViewLinkAsync(
            new ViewLinkRequested(message.ListingId, search.Id, SecureTokens.Hash(token), timeProvider.GetUtcNow() + SearchAlerter.LinkValidity, search.SeekerAddress),
            ct);
        string? reference = await notifier.SendAccessLinkAsync(
            new SeekerAccessLink(search.SeekerAddress, search.Channel, message.Language, message.ListingTitle, token),
            ct);
        if (reference is not null)
        {
            await searches.RecordSentMessageAsync(reference, search.Id, ct);
        }

        logger.LogInformation("Personal link for listing {ListingId} sent to search {SearchId}", message.ListingId, search.Id);
    }

    private async Task<Search> SearchForAsync(ListingAccessRequested message, CancellationToken ct)
    {
        string name = message.City ?? message.ListingTitle;
        IReadOnlyList<Search> existing = await searches.FindBySeekerAsync(message.Address, ct);
        Search? sameName = existing.FirstOrDefault(search => search.IsOpen && string.Equals(search.Name, name, StringComparison.OrdinalIgnoreCase));
        if (sameName is not null)
        {
            return sameName;
        }

        Search created = new(Guid.NewGuid(), message.Address, EmailChannel, name, message.Language, EmptyCriteria, Search.PausedStatus);
        await searches.SaveAsync(created, ct);
        logger.LogInformation("Paused search {SearchId} created for a personal link request", created.Id);
        return created;
    }
}
