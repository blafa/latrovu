using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Searches;
using Npgsql;

namespace Latrovu.Intake.Infrastructure;

public sealed class SearchRequestForwarder(
    IOutbox outbox,
    NpgsqlDataSource dataSource,
    ILogger<SearchRequestForwarder> logger) : ISearchRequestForwarder
{
    private const string EmailChannel = "email";

    public async Task ForwardAsync(InboundMail mail, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mail);

        string text = string.Join('\n', new[] { mail.Subject, QuotedReplyStripper.Strip(mail.Body) }.Where(part => !string.IsNullOrWhiteSpace(part)));
        SearchRequestReceived message = new(Guid.NewGuid(), mail.MessageId, EmailChannel, mail.Sender, text, mail.InReplyTo);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        await outbox.AddAsync([message], transaction, ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation("Search request {RequestId} forwarded", message.RequestId);
    }
}
