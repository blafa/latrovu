using Latrovu.BuildingBlocks.Email;

namespace Latrovu.BuildingBlocks.Infrastructure.Email;

public static class EmailBranding
{
    public const string LogoContentId = "latrovu-symbol";
    private const string LogoResource = "latrovu-symbol-email.png";
    private const string PngMediaType = "image/png";

    public const string HeaderRow =
        "<tr><td style=\"padding:28px 32px 0;\">"
        + $"<img src=\"cid:{LogoContentId}\" width=\"29\" height=\"32\" alt=\"\" style=\"display:inline-block;vertical-align:middle;border:0;\">"
        + "<span style=\"display:inline-block;vertical-align:middle;margin-left:10px;font-size:20px;font-weight:700;letter-spacing:-0.02em;color:#111418;\">"
        + "latrovu<span style=\"color:#9CC41F;\">.</span></span></td></tr>";

    private static readonly Lazy<EmailInlineImage> LogoImage = new(LoadLogo);

    public static IReadOnlyList<EmailInlineImage> InlineImages => [LogoImage.Value];

    private static EmailInlineImage LoadLogo()
    {
        using Stream stream = typeof(EmailBranding).Assembly.GetManifestResourceStream(LogoResource)
            ?? throw new InvalidOperationException($"Embedded resource {LogoResource} is missing");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return new EmailInlineImage(LogoContentId, LogoResource, PngMediaType, buffer.ToArray());
    }
}
