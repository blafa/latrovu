namespace Latrovu.Intake;

public interface IMailDispatcher
{
    Task DispatchAsync(InboundMail mail, CancellationToken ct);
}
