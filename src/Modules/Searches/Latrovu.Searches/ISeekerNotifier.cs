namespace Latrovu.Searches;

public interface ISeekerNotifier
{
    Task SendReplyAsync(SeekerReply reply, CancellationToken ct);

    Task SendAlertAsync(SeekerAlert alert, CancellationToken ct);

    Task SendAgentAnswerAsync(SeekerAgentAnswer answer, CancellationToken ct);
}
