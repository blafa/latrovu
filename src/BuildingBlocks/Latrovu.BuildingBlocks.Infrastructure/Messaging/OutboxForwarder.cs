using Microsoft.Extensions.Hosting;
using Npgsql;
using Rebus.Bus;
using Rebus.Messages;

namespace Latrovu.BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxForwarder(
    NpgsqlDataSource dataSource,
    IBus bus,
    TimeProvider timeProvider,
    ILogger<OutboxForwarder> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private const string SelectSql = """
        SELECT id, type, payload::text FROM outbox
        WHERE sent_at IS NULL ORDER BY created_at LIMIT $1 FOR UPDATE SKIP LOCKED
        """;

    private const string MarkSentSql = "UPDATE outbox SET sent_at = $2 WHERE id = ANY($1)";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox forwarder started");
        using PeriodicTimer timer = new(PollInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TryForwardBatchAsync(stoppingToken);
        }
    }

    private async Task TryForwardBatchAsync(CancellationToken ct)
    {
        try
        {
            int forwarded = await ForwardBatchAsync(ct);
            if (forwarded > 0)
            {
                logger.LogInformation("Forwarded {MessageCount} outbox messages", forwarded);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Forwarding outbox messages failed, retrying with the next batch");
        }
    }

    private async Task<int> ForwardBatchAsync(CancellationToken ct)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        List<Guid> sent = [];
        foreach ((Guid id, string type, string payload) in await ReadBatchAsync(connection, transaction, ct))
        {
            Dictionary<string, string> headers = new() { [Headers.MessageId] = id.ToString() };
            await bus.Publish(OutboxSerializer.Deserialize(type, payload), headers);
            sent.Add(id);
        }

        await MarkSentAsync(connection, transaction, sent, ct);
        await transaction.CommitAsync(ct);
        return sent.Count;
    }

    private static async Task<List<(Guid Id, string Type, string Payload)>> ReadBatchAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using NpgsqlCommand command = new(SelectSql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter { Value = BatchSize });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        List<(Guid, string, string)> batch = [];
        while (await reader.ReadAsync(ct))
        {
            batch.Add((reader.GetGuid(0), reader.GetString(1), reader.GetString(2)));
        }

        return batch;
    }

    private async Task MarkSentAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, List<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return;
        }

        await using NpgsqlCommand command = new(MarkSentSql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter { Value = ids.ToArray() });
        command.Parameters.Add(new NpgsqlParameter { Value = timeProvider.GetUtcNow() });
        await command.ExecuteNonQueryAsync(ct);
    }
}
