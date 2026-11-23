namespace Latrovu.BuildingBlocks.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
