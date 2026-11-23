using Latrovu.Core.Submissions;

namespace Latrovu.Listings.Images;

public interface IListingImageCollector
{
    Task CollectAsync(
        Submission message,
        IReadOnlyList<SubmittedFile> files,
        IReadOnlyList<CreatedListing> createdListings,
        CancellationToken ct);
}
