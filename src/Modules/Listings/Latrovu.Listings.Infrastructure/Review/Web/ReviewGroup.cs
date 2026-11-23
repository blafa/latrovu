namespace Latrovu.Listings.Infrastructure.Review.Web;

internal sealed record ReviewGroup(string Label, string Icon, IReadOnlyList<string> Sections);
