namespace Latrovu.Intake;

public interface ISearchRequestForwarder
{
    Task ForwardAsync(InboundMail mail, CancellationToken ct);
}
