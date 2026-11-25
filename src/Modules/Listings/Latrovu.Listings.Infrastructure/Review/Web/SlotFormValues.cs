namespace Latrovu.Listings.Infrastructure.Review.Web;

internal sealed record SlotFormValues(string Date, string From, string To, string Places)
{
    public static SlotFormValues Empty { get; } = new(string.Empty, string.Empty, string.Empty, "5");
}
