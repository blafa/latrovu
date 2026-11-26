using System.ComponentModel.DataAnnotations;

namespace Latrovu.BuildingBlocks.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";
    public const int ImplicitTlsPort = 465;
    public const string DefaultFromName = "latrovu";

    [Required]
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = ImplicitTlsPort;

    [Required]
    public string User { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = DefaultFromName;
}
