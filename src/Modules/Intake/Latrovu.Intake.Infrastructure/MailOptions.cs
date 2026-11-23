using System.ComponentModel.DataAnnotations;

namespace Latrovu.Intake.Infrastructure;

public sealed class MailOptions
{
    public const string SectionName = "Mail";
    public const int DefaultImapsPort = 993;
    public const string DefaultFolder = "Latrovu-Bot";
    public const int DefaultPollIntervalSeconds = 60;
    public const int MinPollIntervalSeconds = 5;
    public const int MaxPollIntervalSeconds = 3600;

    [Required]
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = DefaultImapsPort;

    [Required]
    public string User { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public string Folder { get; init; } = DefaultFolder;

    [Range(MinPollIntervalSeconds, MaxPollIntervalSeconds)]
    public int PollIntervalSeconds { get; init; } = DefaultPollIntervalSeconds;
}
