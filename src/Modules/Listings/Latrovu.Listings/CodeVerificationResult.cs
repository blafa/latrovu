namespace Latrovu.Listings;

public sealed record CodeVerificationResult(CodeVerificationOutcome Outcome, string? SessionToken)
{
    public static CodeVerificationResult Failed(CodeVerificationOutcome outcome) => new(outcome, null);
}
