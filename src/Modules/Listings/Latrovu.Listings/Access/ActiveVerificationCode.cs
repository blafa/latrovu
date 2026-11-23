namespace Latrovu.Listings.Access;

public sealed record ActiveVerificationCode(Guid Id, string CodeHash, int Attempts);
