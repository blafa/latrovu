namespace Latrovu.Listings.Access;

public interface IVerificationCodeSender
{
    Task SendAsync(string recipient, string code, CancellationToken ct);
}
