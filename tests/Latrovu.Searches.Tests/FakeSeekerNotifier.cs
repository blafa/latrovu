namespace Latrovu.Searches.Tests;

internal sealed class FakeSeekerNotifier : ISeekerNotifier
{
    public const string SentReference = "sent@latrovu.test";

    public List<SeekerViewingConfirmation> ViewingConfirmations { get; } = [];

    public List<SeekerAccessLink> AccessLinks { get; } = [];

    public Task<string?> SendAccessLinkAsync(SeekerAccessLink link, CancellationToken ct)
    {
        AccessLinks.Add(link);
        return Task.FromResult<string?>(SentReference);
    }

    public Task<string?> SendViewingConfirmationAsync(SeekerViewingConfirmation confirmation, CancellationToken ct)
    {
        ViewingConfirmations.Add(confirmation);
        return Task.FromResult<string?>(SentReference);
    }

    public Task<string?> SendReplyAsync(SeekerReply reply, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<string?> SendAlertAsync(SeekerAlert alert, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task SendAgentAnswerAsync(SeekerAgentAnswer answer, CancellationToken ct) =>
        throw new NotSupportedException();
}
