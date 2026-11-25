namespace Latrovu.Core.Listings;

public sealed record AgentReplyReceived(
    Guid ReplyId,
    string SourceMessageId,
    string Reference,
    string SenderAddress,
    string Text);
