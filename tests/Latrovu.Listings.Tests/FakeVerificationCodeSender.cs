using Latrovu.Listings;

namespace Latrovu.Listings.Tests;

internal sealed class FakeVerificationCodeSender : IVerificationCodeSender
{
    public string? LastRecipient { get; private set; }

    public string? LastCode { get; private set; }

    public Task SendAsync(string recipient, string code, CancellationToken ct)
    {
        LastRecipient = recipient;
        LastCode = code;
        return Task.CompletedTask;
    }
}
