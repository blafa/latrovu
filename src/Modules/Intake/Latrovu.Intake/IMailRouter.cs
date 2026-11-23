namespace Latrovu.Intake;

public interface IMailRouter
{
    MailKind Classify(IEnumerable<string> recipients);
}
