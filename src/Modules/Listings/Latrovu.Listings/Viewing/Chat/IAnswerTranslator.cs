namespace Latrovu.Listings.Viewing.Chat;

public interface IAnswerTranslator
{
    Task<TranslatedAnswer> PrepareForSeekerAsync(
        string answer,
        IReadOnlyList<string> questions,
        string listingDataJson,
        CancellationToken ct);
}
