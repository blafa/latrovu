using System.Data.Common;
using Latrovu.BuildingBlocks.Messaging;
using Npgsql;

namespace Latrovu.BuildingBlocks.Infrastructure.Messaging;

public sealed class PostgresOutbox(TimeProvider timeProvider) : IOutbox
{
    private const string InsertSql = "INSERT INTO outbox (id, type, payload, created_at) VALUES ($1, $2, $3::jsonb, $4)";

    public async Task AddAsync(IEnumerable<object> messages, DbTransaction transaction, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (transaction is not NpgsqlTransaction npgsqlTransaction || npgsqlTransaction.Connection is null)
        {
            throw new ArgumentException("The outbox requires an open Npgsql transaction", nameof(transaction));
        }

        foreach (object message in messages)
        {
            (string typeName, string payload) = OutboxSerializer.Serialize(message);
            await using NpgsqlCommand command = new(InsertSql, npgsqlTransaction.Connection, npgsqlTransaction);
            command.Parameters.Add(new NpgsqlParameter { Value = Guid.NewGuid() });
            command.Parameters.Add(new NpgsqlParameter { Value = typeName });
            command.Parameters.Add(new NpgsqlParameter { Value = payload });
            command.Parameters.Add(new NpgsqlParameter { Value = timeProvider.GetUtcNow() });
            await command.ExecuteNonQueryAsync(ct);
        }
    }
}
