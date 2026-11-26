using System.ComponentModel.DataAnnotations;

namespace Latrovu.Listings.Infrastructure;

public sealed class PublicLinkOptions
{
    public const string SectionName = "Links";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;

    public string ReviewUrl(string token) => $"{BaseUrl.TrimEnd('/')}/review/{token}";

    public string RenewUrl(string token) => $"{BaseUrl.TrimEnd('/')}/renew/{token}";
}
