using System.ComponentModel.DataAnnotations;

namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public sealed class QuestionForwardingOptions
{
    public const string SectionName = "QuestionForwarding";

    [Required]
    [EmailAddress]
    public string ReplyToAddress { get; init; } = "inserat@latrovu.com";

    [Range(0, 60)]
    public int BundleMinutes { get; init; } = 2;
}
