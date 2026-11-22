namespace Latrovu.Api.Mail;

public interface IMailInbox
{
    Task ProcessUnreadAsync(Func<InboundMail, CancellationToken, Task> handleMail, CancellationToken ct);
}
