namespace Latrovu.Core.Messaging;

public sealed record ReceivedMessage(string MessageId, string Sender, string? Subject);
