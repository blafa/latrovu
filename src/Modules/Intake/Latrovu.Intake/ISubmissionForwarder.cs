namespace Latrovu.Intake;

public interface ISubmissionForwarder
{
    Task ForwardAsync(InboundMail mail, CancellationToken ct);
}
