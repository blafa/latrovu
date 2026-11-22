namespace Latrovu.Api.Mail;

public interface IMailRouter
{
    MailKind Classify(IEnumerable<string> recipients);
}
