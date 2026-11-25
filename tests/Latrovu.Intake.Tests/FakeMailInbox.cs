namespace Latrovu.Intake.Tests;

internal sealed class FakeMailInbox : IMailInbox
{
    public List<string> DeletedMessageIds { get; } = [];

    public int Calls { get; private set; }

    public Task ProcessPendingAsync(Func<InboundMail, CancellationToken, Task> handleMail, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<int> DeleteProcessedAsync(IReadOnlyCollection<string> messageIds, CancellationToken ct)
    {
        Calls++;
        DeletedMessageIds.AddRange(messageIds);
        return Task.FromResult(messageIds.Count);
    }
}
