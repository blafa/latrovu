using System.Globalization;
using Latrovu.BuildingBlocks.Email;

namespace Latrovu.Listings.Infrastructure.Access;

public sealed class EmailVerificationCodeSender(IEmailSender emailSender) : IVerificationCodeSender
{
    private const string Subject = "Ihr Bestätigungscode für Latrovu";

    private const string BodyTemplate = """
        Guten Tag

        Ihr Code zum Öffnen Ihres Inserats bei Latrovu lautet:

            {0}

        Der Code ist {1} Minuten gültig. Falls Sie ihn nicht angefordert haben, können Sie diese E-Mail ignorieren.

        Freundliche Grüsse
        Latrovu
        """;

    public Task SendAsync(string recipient, string code, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        string body = string.Format(
            CultureInfo.InvariantCulture,
            BodyTemplate,
            code,
            ReviewAccessService.CodeValidity.TotalMinutes);
        return emailSender.SendAsync(new EmailMessage(recipient, Subject, body), ct);
    }
}
