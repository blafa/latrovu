namespace Latrovu.Listings;

public sealed record ActiveVerificationCode(Guid Id, string CodeHash, int Attempts);
