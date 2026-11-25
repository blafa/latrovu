namespace Latrovu.Intake;

public interface IAgentReplyForwarder
{
    Task ForwardAsync(InboundMail mail, string reference, CancellationToken ct);
}
