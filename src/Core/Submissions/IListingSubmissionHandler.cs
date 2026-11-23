namespace Latrovu.Core.Submissions;

public interface IListingSubmissionHandler
{
    Task HandleAsync(Submission message, IReadOnlyList<SubmittedFile> files, CancellationToken ct);
}
