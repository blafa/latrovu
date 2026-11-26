using Latrovu.BuildingBlocks.Email;
using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.Listings.Infrastructure.Review.Web;
using Microsoft.Extensions.Options;

namespace Latrovu.Listings.Infrastructure.Ingestion;

public sealed class EmailIntakeReplyNotifier(
    IEmailSender emailSender,
    IListingReviewStore reviews,
    IOptions<PublicLinkOptions> links,
    ILogger<EmailIntakeReplyNotifier> logger) : IIntakeReplyNotifier
{
    public async Task SendAsync(IntakeReport report, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (string.IsNullOrWhiteSpace(report.Recipient))
        {
            logger.LogWarning("Intake reply not sent, the submission has no sender address");
            return;
        }

        IntakeReplyContent content = new(report, await LoadSummariesAsync(report, ct), links.Value.ReviewUrl);
        await emailSender.SendAsync(
            new EmailMessage(
                report.Recipient,
                IntakeReplyMail.Subject(report),
                IntakeReplyMail.Text(content),
                IntakeReplyMail.Html(content),
                InlineImages: EmailBranding.InlineImages,
                IsAutoReply: true),
            ct);
        logger.LogInformation(
            "Intake reply sent for {FileCount} files, {ListingCount} listings",
            report.Files.Count,
            report.Files.Count(file => file.Status != IntakeFileStatus.NotAListing));
    }

    private async Task<Dictionary<Guid, IntakeListingSummary>> LoadSummariesAsync(IntakeReport report, CancellationToken ct)
    {
        Dictionary<Guid, IntakeListingSummary> summaries = [];
        foreach (Guid listingId in report.Files.Select(file => file.ListingId).OfType<Guid>().Distinct())
        {
            if (await reviews.GetAsync(listingId, ct) is { } review)
            {
                summaries[listingId] = IntakeListingSummary.From(review);
            }
        }

        return summaries;
    }
}
