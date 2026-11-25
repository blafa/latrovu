using Latrovu.Intake;

namespace Latrovu.Hosts.Intake.Dev;

public static class DevMailEndpoint
{
    private const string Route = "/dev/mail";
    private const string DefaultRecipient = "inserat@latrovu.com";
    private const string SenderSettingKey = "DevImport:Sender";
    private const string DefaultSender = "dev-mail@latrovu.local";

    public static void MapDevMail(this WebApplication app)
    {
        app.MapPost(Route, SubmitAsync);
    }

    private static async Task<IResult> SubmitAsync(
        DevMailRequest request,
        IConfiguration config,
        IMailDispatcher dispatcher,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.BadRequest("Body is required");
        }

        string sender = request.Sender ?? config[SenderSettingKey] ?? DefaultSender;
        InboundMail mail = new($"dev-mail:{Guid.NewGuid()}", sender, request.Subject, [request.To ?? DefaultRecipient], [], request.Body);
        await dispatcher.DispatchAsync(mail, ct);
        return Results.Accepted();
    }
}
