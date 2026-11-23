namespace Latrovu.Listings;

public enum CodeVerificationOutcome
{
    Verified,
    Invalid,
    Expired,
    TooManyAttempts,
    NotFound
}
