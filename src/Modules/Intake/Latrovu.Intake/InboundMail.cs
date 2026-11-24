using Latrovu.Core.Submissions;

namespace Latrovu.Intake;

public sealed record InboundMail(
    string MessageId,
    string Sender,
    string? Subject,
    IReadOnlyList<string> Recipients,
    IReadOnlyList<SubmittedFile> Attachments);
