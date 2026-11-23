namespace Latrovu.Intake;

public interface IMailInbox
{
    Task ProcessUnreadAsync(Func<InboundMail, CancellationToken, Task> handleMail, CancellationToken ct);
}
