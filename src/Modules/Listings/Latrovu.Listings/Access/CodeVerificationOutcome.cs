namespace Latrovu.Listings.Access;

public enum CodeVerificationOutcome
{
    Verified,
    Invalid,
    Expired,
    TooManyAttempts,
    NotFound
}
