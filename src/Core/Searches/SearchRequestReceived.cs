namespace Latrovu.Core.Searches;

public sealed record SearchRequestReceived(
    Guid RequestId,
    string SourceMessageId,
    string Channel,
    string SenderAddress,
    string Text,
    string? ReplyToReference);
