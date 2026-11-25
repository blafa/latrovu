namespace Latrovu.Intake;

public interface IMailInbox
{
    Task ProcessPendingAsync(Func<InboundMail, CancellationToken, Task> handleMail, CancellationToken ct);
}
