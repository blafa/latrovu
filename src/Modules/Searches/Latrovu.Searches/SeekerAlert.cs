using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public sealed record SeekerAlert(
    string Address,
    string Channel,
    string Language,
    string SearchName,
    ListingReadyForMatching Listing,
    string Token);
