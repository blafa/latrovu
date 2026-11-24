using System.Diagnostics;
using Latrovu.BuildingBlocks.Email;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Latrovu.BuildingBlocks.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions settings = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        Stopwatch stopwatch = Stopwatch.StartNew();
        using SmtpClient client = new();
        SecureSocketOptions security = settings.Port == SmtpOptions.ImplicitTlsPort
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
        await client.ConnectAsync(settings.Host, settings.Port, security, ct);
        await client.AuthenticateAsync(settings.User, settings.Password, ct);
        await client.SendAsync(new SendRequest(ToMimeMessage(message)), ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation("Email sent via {Host} in {ElapsedMs} ms", settings.Host, stopwatch.ElapsedMilliseconds);
    }

    private MimeMessage ToMimeMessage(EmailMessage message)
    {
        string fromAddress = string.IsNullOrWhiteSpace(settings.FromAddress) ? settings.User : settings.FromAddress;
        MimeMessage mime = new();
        mime.From.Add(new MailboxAddress(settings.FromName, fromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
        {
            mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
        }

        mime.Subject = message.Subject;
        mime.Body = BuildBody(message);
        return mime;
    }

    private static MimeEntity BuildBody(EmailMessage message)
    {
        BodyBuilder builder = new() { TextBody = message.TextBody, HtmlBody = message.HtmlBody };
        foreach (EmailInlineImage image in message.InlineImages ?? [])
        {
            MimeEntity resource = builder.LinkedResources.Add(image.FileName, image.Content, ContentType.Parse(image.MediaType));
            resource.ContentId = image.ContentId;
        }

        return builder.ToMessageBody();
    }
}
