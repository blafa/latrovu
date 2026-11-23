namespace Latrovu.BuildingBlocks.Email;

public sealed record EmailMessage(string To, string Subject, string TextBody);
