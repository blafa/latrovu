using Latrovu.Api.Mail;

namespace Latrovu.Api.Listings;

public interface IListingImageCollector
{
    Task CollectAsync(InboundMail mail, IReadOnlyList<CreatedListing> createdListings, CancellationToken ct);
}
