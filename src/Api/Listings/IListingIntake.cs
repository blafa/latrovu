using Latrovu.Api.Mail;

namespace Latrovu.Api.Listings;

public interface IListingIntake
{
    Task HandleAsync(InboundMail mail, CancellationToken ct);
}
