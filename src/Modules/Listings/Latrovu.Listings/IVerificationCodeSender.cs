namespace Latrovu.Listings;

public interface IVerificationCodeSender
{
    Task SendAsync(string recipient, string code, CancellationToken ct);
}
