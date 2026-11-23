using Latrovu.Core.Messaging;

namespace Latrovu.Intake;

public sealed record InboundMail(
    string MessageId,
    string Sender,
    string? Subject,
    IReadOnlyList<string> Recipients,
    IReadOnlyList<ReceivedFile> Attachments)
{
    public ReceivedMessage ToReceivedMessage() => new(MessageId, Sender, Subject);
}
