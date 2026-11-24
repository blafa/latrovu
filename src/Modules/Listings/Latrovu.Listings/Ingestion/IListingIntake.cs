using Latrovu.Core.Submissions;

namespace Latrovu.Listings.Ingestion;

public interface IListingIntake
{
    Task HandleAsync(Submission message, IReadOnlyList<SubmittedFile> files, CancellationToken ct);
}
