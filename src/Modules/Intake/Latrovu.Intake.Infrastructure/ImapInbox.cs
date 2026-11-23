using System.Diagnostics;
using Latrovu.Core.Messaging;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Latrovu.Intake.Infrastructure;

public sealed class ImapInbox(IOptions<MailOptions> options, ILogger<ImapInbox> logger) : IMailInbox
{
    private readonly MailOptions settings = options.Value;

    public async Task ProcessUnreadAsync(Func<InboundMail, CancellationToken, Task> handleMail, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(handleMail);

        using ImapClient client = await ConnectAsync(ct);
        IMailFolder folder = await OpenFolderAsync(client, ct);
        IList<UniqueId> unread = await folder.SearchAsync(SearchQuery.NotSeen, ct);
        logger.LogDebug("Found {UnreadCount} unread mails in {Folder}", unread.Count, settings.Folder);

        foreach (UniqueId uid in unread)
        {
            MimeMessage message = await folder.GetMessageAsync(uid, ct);
            InboundMail mail = await ToInboundMailAsync(message, ct);
            await handleMail(mail, ct);
            await folder.AddFlagsAsync(uid, MessageFlags.Seen, true, ct);
        }

        await client.DisconnectAsync(true, ct);
    }

    private async Task<ImapClient> ConnectAsync(CancellationToken ct)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        ImapClient client = new();
        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.SslOnConnect, ct);
            await client.AuthenticateAsync(settings.User, settings.Password, ct);
        }
        catch
        {
            client.Dispose();
            throw;
        }

        logger.LogDebug("Connected to {Host} in {ElapsedMs} ms", settings.Host, stopwatch.ElapsedMilliseconds);
        return client;
    }

    private async Task<IMailFolder> OpenFolderAsync(ImapClient client, CancellationToken ct)
    {
        IMailFolder folder;
        try
        {
            folder = await client.GetFolderAsync(settings.Folder, ct);
        }
        catch (FolderNotFoundException)
        {
            logger.LogDebug("Folder {Folder} not found at top level, looking below the inbox", settings.Folder);
            folder = await client.Inbox.GetSubfolderAsync(settings.Folder, ct);
        }

        await folder.OpenAsync(FolderAccess.ReadWrite, ct);
        return folder;
    }

    private static async Task<InboundMail> ToInboundMailAsync(MimeMessage message, CancellationToken ct)
    {
        List<ReceivedFile> attachments = [];
        foreach (MimePart part in message.Attachments.OfType<MimePart>())
        {
            attachments.Add(await ToReceivedFileAsync(part, ct));
        }

        List<string> recipients = message.To.Mailboxes
            .Concat(message.Cc.Mailboxes)
            .Select(mailbox => mailbox.Address)
            .ToList();

        return new InboundMail(
            message.MessageId ?? string.Empty,
            message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
            message.Subject,
            recipients,
            attachments);
    }

    private static async Task<ReceivedFile> ToReceivedFileAsync(MimePart part, CancellationToken ct)
    {
        string fileName = part.FileName ?? string.Empty;
        if (part.Content is null)
        {
            return new ReceivedFile(fileName, part.ContentType.MimeType, []);
        }

        using MemoryStream stream = new();
        await part.Content.DecodeToAsync(stream, ct);
        return new ReceivedFile(fileName, part.ContentType.MimeType, stream.ToArray());
    }
}
