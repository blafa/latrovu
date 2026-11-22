using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;

namespace Latrovu.Api;

// Reads unseen mails from the bot folder every 60 seconds,
// decides by the "To" address whether it is a listing or a search,
// logs it and marks it as seen.
public sealed class MailPoller(IConfiguration config, ILogger<MailPoller> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await PollOnce(ct); }
            catch (Exception ex) { log.LogError(ex, "Mail poll failed"); }
            await Task.Delay(TimeSpan.FromSeconds(60), ct);
        }
    }

    private async Task PollOnce(CancellationToken ct)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(config["Mail:Host"], 993, SecureSocketOptions.SslOnConnect, ct);
        await client.AuthenticateAsync(config["Mail:User"], config["Mail:Password"], ct);

        var name = config["Mail:Folder"] ?? "Latrovu-Bot";
        IMailFolder folder;
        try { folder = await client.GetFolderAsync(name, ct); }
        catch (FolderNotFoundException) { folder = await client.Inbox.GetSubfolderAsync(name, ct); }
        await folder.OpenAsync(FolderAccess.ReadWrite, ct);

        foreach (var uid in await folder.SearchAsync(SearchQuery.NotSeen, ct))
        {
            var msg = await folder.GetMessageAsync(uid, ct);
            var to = string.Join(", ", msg.To.Mailboxes.Select(m => m.Address)).ToLowerInvariant();
            var kind = to.Contains("inserat@") ? "LISTING" : to.Contains("suche@") ? "SEARCH" : "UNKNOWN";

            log.LogInformation("{Kind} from {From}: {Subject} ({Count} attachments)",
                kind, msg.From.Mailboxes.FirstOrDefault()?.Address, msg.Subject, msg.Attachments.Count());

            await folder.AddFlagsAsync(uid, MessageFlags.Seen, true, ct);
        }

        await client.DisconnectAsync(true, ct);
    }
}
