namespace Latrovu.Api.Mail;

public sealed class MailRouter : IMailRouter
{
    private const string ListingMailbox = "inserat";
    private const string SearchMailbox = "suche";
    private const char AddressSeparator = '@';

    public MailKind Classify(IEnumerable<string> recipients)
    {
        ArgumentNullException.ThrowIfNull(recipients);

        List<string> mailboxes = recipients.Select(ToMailbox).ToList();
        if (mailboxes.Contains(ListingMailbox))
        {
            return MailKind.Listing;
        }

        if (mailboxes.Contains(SearchMailbox))
        {
            return MailKind.Search;
        }

        return MailKind.Unknown;
    }

    private static string ToMailbox(string address)
    {
        int separatorIndex = address.IndexOf(AddressSeparator);
        string mailbox = separatorIndex < 0 ? address : address[..separatorIndex];
        return mailbox.Trim().ToLowerInvariant();
    }
}
