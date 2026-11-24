using System.ComponentModel.DataAnnotations;

namespace Latrovu.Searches.Infrastructure;

public sealed class AlertOptions
{
    public const string SectionName = "Alerts";

    [Required]
    [Url]
    public string ViewBaseUrl { get; init; } = string.Empty;
}
