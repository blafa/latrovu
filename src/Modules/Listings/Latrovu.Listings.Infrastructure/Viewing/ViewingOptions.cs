using System.ComponentModel.DataAnnotations;

namespace Latrovu.Listings.Infrastructure.Viewing;

public sealed class ViewingOptions
{
    public const string SectionName = "Viewing";

    [Required]
    [EmailAddress]
    public string InquiryAddress { get; init; } = string.Empty;
}
