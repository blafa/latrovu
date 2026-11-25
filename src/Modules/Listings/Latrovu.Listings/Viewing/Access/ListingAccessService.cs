using System.Net.Mail;
using System.Text.Json.Nodes;
using Latrovu.Core.Listings;

namespace Latrovu.Listings.Viewing.Access;

public sealed class ListingAccessService(
    IListingViewService views,
    IListingAccessRequestStore requests,
    TimeProvider timeProvider,
    ILogger<ListingAccessService> logger)
{
    public const int MaxRequestsPerLink = 10;
    public const int MaxRequestsPerAddress = 3;
    public const int MaxAddressLength = 320;
    public static readonly TimeSpan AddressWindow = TimeSpan.FromHours(1);

    public async Task<AccessOutcome> RequestAsync(string token, string address, string language, CancellationToken ct)
    {
        string? normalized = Normalize(address);
        if (normalized is null)
        {
            return AccessOutcome.InvalidAddress;
        }

        ListingReview? listing = await views.FindSharedAsync(token, ct);
        if (listing is null)
        {
            return AccessOutcome.NotFound;
        }

        string tokenHash = AccessTokens.Hash(token);
        DateTimeOffset now = timeProvider.GetUtcNow();
        AccessRequestCounts counts = await requests.CountAsync(tokenHash, normalized, now - AddressWindow, ct);
        if (counts.ForLink >= MaxRequestsPerLink || counts.ForAddress >= MaxRequestsPerAddress)
        {
            logger.LogWarning("Access request for listing {ListingId} refused, limit reached ({ForLink} for link, {ForAddress} for address)",
                listing.Id, counts.ForLink, counts.ForAddress);
            return AccessOutcome.LimitReached;
        }

        (string title, string? city) = Describe(listing);
        await requests.RecordAsync(tokenHash, new ListingAccessRequested(listing.Id, normalized, language, title, city), now, ct);
        logger.LogInformation("Personal link requested for listing {ListingId} in {Language}", listing.Id, language);
        return AccessOutcome.Sent;
    }

    private static string? Normalize(string address)
    {
        string trimmed = address?.Trim() ?? string.Empty;
        return trimmed.Length <= MaxAddressLength && MailAddress.TryCreate(trimmed, out MailAddress? mail) && mail.Address == trimmed
            ? trimmed
            : null;
    }

    private static (string Title, string? City) Describe(ListingReview listing)
    {
        JsonObject data = JsonNode.Parse(listing.DataJson) as JsonObject ?? [];
        return (TextOf(data["title"]) ?? string.Empty, TextOf(data["location"]?["city"]));
    }

    private static string? TextOf(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue(out string? text) && !string.IsNullOrWhiteSpace(text) ? text : null;
}
