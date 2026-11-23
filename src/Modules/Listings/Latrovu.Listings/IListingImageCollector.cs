using Latrovu.Core.Messaging;

namespace Latrovu.Listings;

public interface IListingImageCollector
{
    Task CollectAsync(
        ReceivedMessage message,
        IReadOnlyList<ReceivedFile> files,
        IReadOnlyList<CreatedListing> createdListings,
        CancellationToken ct);
}
