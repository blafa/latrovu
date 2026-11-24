namespace Latrovu.Searches;

public interface ISeekerNotifier
{
    Task SendReplyAsync(SeekerReply reply, CancellationToken ct);
}
