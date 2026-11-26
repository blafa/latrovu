using Latrovu.BuildingBlocks.Email;
using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.Listings.Renewal;
using Microsoft.Extensions.Options;

namespace Latrovu.Listings.Infrastructure.Renewal;

public sealed class EmailRenewalNotifier(
    IEmailSender emailSender,
    IOptions<PublicLinkOptions> links,
    ILogger<EmailRenewalNotifier> logger) : IRenewalNotifier
{
    public async Task SendReminderAsync(RenewalReminder reminder, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        string renewUrl = links.Value.RenewUrl(reminder.RenewToken);
        string reviewUrl = links.Value.ReviewUrl(reminder.ReviewToken);
        await emailSender.SendAsync(
            new EmailMessage(
                reminder.AgentAddress,
                RenewalMail.Subject(reminder),
                RenewalMail.Text(reminder, renewUrl, reviewUrl),
                RenewalMail.Html(reminder, renewUrl, reviewUrl),
                null,
                EmailBranding.InlineImages),
            ct);
        logger.LogInformation("Renewal reminder {Number} mailed", reminder.Number);
    }
}
