using Latrovu.Intake;

namespace Latrovu.Hosts.Intake.Dev;

public static class DevSearchEndpoint
{
    private const string Route = "/dev/search";
    private const string SearchRecipient = "suche@latrovu.com";
    private const string SenderSettingKey = "DevImport:Sender";
    private const string DefaultSender = "dev-search@latrovu.local";

    public static void MapDevSearch(this WebApplication app)
    {
        app.MapPost(Route, SubmitAsync);
    }

    private static async Task<IResult> SubmitAsync(
        DevSearchRequest request,
        IConfiguration config,
        IMailDispatcher dispatcher,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Results.BadRequest("Text is required");
        }

        string sender = request.Sender ?? config[SenderSettingKey] ?? DefaultSender;
        InboundMail mail = new($"dev-search:{Guid.NewGuid()}", sender, null, [SearchRecipient], [], request.Text);
        await dispatcher.DispatchAsync(mail, ct);
        return Results.Accepted();
    }
}
