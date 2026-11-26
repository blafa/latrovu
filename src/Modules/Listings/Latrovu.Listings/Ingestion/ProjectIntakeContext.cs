using Latrovu.Core.Submissions;

namespace Latrovu.Listings.Ingestion;

internal sealed record ProjectIntakeContext(
    Submission Message,
    SubmittedFile Pdf,
    IReadOnlyList<string> Pages,
    string SourceHash,
    Guid ProjectId,
    ListingExtraction? Building);
