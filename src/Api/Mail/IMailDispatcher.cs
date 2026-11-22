namespace Latrovu.Api.Mail;

public interface IMailDispatcher
{
    Task DispatchAsync(InboundMail mail, CancellationToken ct);
}
