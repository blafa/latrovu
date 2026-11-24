namespace Latrovu.Core.Submissions;

public sealed record SubmissionReceived(
    Guid SubmissionId,
    string SourceMessageId,
    string Sender,
    string? Subject,
    IReadOnlyList<SubmittedFileRef> Files)
{
    public Submission ToSubmission() => new(SourceMessageId, Sender, Subject);
}
