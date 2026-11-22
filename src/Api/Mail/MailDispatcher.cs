using Latrovu.Api.Listings;

namespace Latrovu.Api.Mail;

public sealed class MailDispatcher(
    IMailRouter router,
    IListingIntake listingIntake,
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
                await listingIntake.HandleAsync(mail, ct);
                break;
            case MailKind.Search:
                logger.LogWarning("Search mail {MessageId} received, search intake is not implemented yet", mail.MessageId);
                break;
            default:
                logger.LogWarning("Mail {MessageId} has no known recipient and is ignored", mail.MessageId);
                break;
        }
    }
}
