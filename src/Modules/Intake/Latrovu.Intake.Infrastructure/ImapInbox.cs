using System.Diagnostics;
using Latrovu.Core.Submissions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Latrovu.Intake.Infrastructure;

public sealed class ImapInbox(IOptions<MailOptions> options, ILogger<ImapInbox> logger) : IMailInbox
{
    private const string AutoSubmittedHeader = "Auto-Submitted";
    private const string PrecedenceHeader = "Precedence";
    private const string AutoReplyHeader = "X-Autoreply";
    private const string AutoRespondHeader = "X-Autorespond";

    private readonly MailOptions settings = options.Value;

    public async Task ProcessPendingAsync(Func<InboundMail, CancellationToken, Task> handleMail, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(handleMail);

        using ImapClient client = await ConnectAsync(ct);
        IMailFolder folder = await OpenFolderAsync(client, ct);
        IList<UniqueId> pending = await folder.SearchAsync(SearchQuery.NotDeleted, ct);
        logger.LogDebug("Found {PendingCount} pending mails in {Folder}", pending.Count, settings.Folder);

        foreach (UniqueId uid in pending)
        {
            MimeMessage message = await folder.GetMessageAsync(uid, ct);
            await handleMail(await ToInboundMailAsync(message, ct), ct);
            await folder.AddFlagsAsync(uid, MessageFlags.Deleted, true, ct);
            await folder.ExpungeAsync([uid], ct);
            logger.LogDebug("Processed mail deleted from {Folder}", settings.Folder);
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
        List<SubmittedFile> attachments = [];
        foreach (MimePart part in message.Attachments.OfType<MimePart>())
        {
            attachments.Add(await ToReceivedFileAsync(part, ct));
        }

        List<string> recipients = message.To.Mailboxes
            .Concat(message.Cc.Mailboxes)
            .Select(mailbox => mailbox.Address)
            .ToList();

        string sender = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty;
        return new InboundMail(
            message.MessageId ?? string.Empty,
            sender,
            message.Subject,
            recipients,
            attachments,
            message.TextBody ?? string.Empty,
            message.InReplyTo,
            IsAutomatic(message, sender));
    }

    private static bool IsAutomatic(MimeMessage message, string sender) =>
        AutomaticMail.IsAutomatic(
            message.Headers[AutoSubmittedHeader],
            message.Headers[PrecedenceHeader],
            message.Headers.Contains(AutoReplyHeader) || message.Headers.Contains(AutoRespondHeader),
            sender);

    private static async Task<SubmittedFile> ToReceivedFileAsync(MimePart part, CancellationToken ct)
    {
        string fileName = part.FileName ?? string.Empty;
        if (part.Content is null)
        {
            return new SubmittedFile(fileName, part.ContentType.MimeType, []);
        }

        using MemoryStream stream = new();
        await part.Content.DecodeToAsync(stream, ct);
        return new SubmittedFile(fileName, part.ContentType.MimeType, stream.ToArray());
    }
}
