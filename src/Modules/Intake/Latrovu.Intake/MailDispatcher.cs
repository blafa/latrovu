namespace Latrovu.Intake;

public sealed class MailDispatcher(
    IMailRouter router,
    ISubmissionForwarder forwarder,
    ISearchRequestForwarder searchForwarder,
    ILogger<MailDispatcher> logger) : IMailDispatcher
{
    public async Task DispatchAsync(InboundMail mail, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mail);

        MailKind kind = router.Classify(mail.Recipients);
        logger.LogInformation(
            "Dispatching mail {MessageId} as {MailKind} with {AttachmentCount} attachments",
            mail.MessageId,
            kind,
            mail.Attachments.Count);

        switch (kind)
        {
            case MailKind.Listing:
                await forwarder.ForwardAsync(mail, ct);
                break;
            case MailKind.Search:
                await searchForwarder.ForwardAsync(mail, ct);
                break;
            default:
                logger.LogWarning("Mail {MessageId} has no known recipient and is ignored", mail.MessageId);
                break;
        }
    }
}
