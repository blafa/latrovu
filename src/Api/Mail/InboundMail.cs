namespace Latrovu.Api.Mail;

public sealed record InboundMail(
    string MessageId,
    string Sender,
    string? Subject,
    IReadOnlyList<string> Recipients,
    IReadOnlyList<MailAttachment> Attachments);
