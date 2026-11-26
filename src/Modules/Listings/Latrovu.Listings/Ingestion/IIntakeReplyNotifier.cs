namespace Latrovu.Listings.Ingestion;

public interface IIntakeReplyNotifier
{
    Task SendAsync(IntakeReport report, CancellationToken ct);
}
