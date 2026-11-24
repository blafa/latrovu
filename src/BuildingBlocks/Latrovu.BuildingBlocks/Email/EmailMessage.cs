namespace Latrovu.BuildingBlocks.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string TextBody,
    string? HtmlBody = null,
    string? ReplyTo = null,
    IReadOnlyList<EmailInlineImage>? InlineImages = null);
