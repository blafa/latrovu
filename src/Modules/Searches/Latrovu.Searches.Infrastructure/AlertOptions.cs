using System.ComponentModel.DataAnnotations;

namespace Latrovu.Searches.Infrastructure;

public sealed class AlertOptions
{
    public const string SectionName = "Alerts";
    public const string DefaultReplyToAddress = "suche@latrovu.com";

    [Required]
    [Url]
    public string ViewBaseUrl { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string ReplyToAddress { get; init; } = DefaultReplyToAddress;
}
