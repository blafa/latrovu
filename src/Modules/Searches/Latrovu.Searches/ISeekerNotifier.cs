namespace Latrovu.Searches;

public interface ISeekerNotifier
{
    Task<string?> SendReplyAsync(SeekerReply reply, CancellationToken ct);

    Task<string?> SendAlertAsync(SeekerAlert alert, CancellationToken ct);

    Task SendAgentAnswerAsync(SeekerAgentAnswer answer, CancellationToken ct);

    Task<string?> SendViewingConfirmationAsync(SeekerViewingConfirmation confirmation, CancellationToken ct);

    Task<string?> SendAccessLinkAsync(SeekerAccessLink link, CancellationToken ct);
}
