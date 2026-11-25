using System.ComponentModel.DataAnnotations;

namespace Latrovu.Listings.Infrastructure.Renewal;

public sealed class RenewalOptions
{
    public const string SectionName = "Renewal";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;
}
