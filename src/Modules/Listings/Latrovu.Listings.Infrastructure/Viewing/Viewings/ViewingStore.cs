using Latrovu.Listings.Viewing.Viewings;
using Npgsql;
using NpgsqlTypes;

namespace Latrovu.Listings.Infrastructure.Viewing.Viewings;

public sealed class ViewingStore(NpgsqlDataSource dataSource) : IViewingStore
{
    private const string AnonymousAttendee = "Interessent ohne E-Mail-Adresse";

    private const string SlotsSql = """
        SELECT s.id, s.starts_at, s.ends_at, s.capacity, count(b.token_hash),
               coalesce(bool_or(b.token_hash = $2), false),
               coalesce(array_agg(coalesce(v.seeker_address, $4)) FILTER (WHERE b.token_hash IS NOT NULL), '{}')
        FROM listing_viewing_slots s
        LEFT JOIN listing_viewing_bookings b ON b.slot_id = s.id
        LEFT JOIN listing_view_links v ON v.token_hash = b.token_hash
        WHERE s.listing_id = $1 AND s.ends_at > $3
        GROUP BY s.id ORDER BY s.starts_at
        """;

    private const string AddSql = """
        INSERT INTO listing_viewing_slots (listing_id, starts_at, ends_at, capacity) VALUES ($1, $2, $3, $4) RETURNING id
        """;

    private const string RemoveEmptySql = """
        DELETE FROM listing_viewing_slots s WHERE s.listing_id = $1 AND s.id = $2
          AND NOT EXISTS (SELECT 1 FROM listing_viewing_bookings b WHERE b.slot_id = s.id)
        """;

    private const string LockSlotSql = "SELECT capacity, ends_at FROM listing_viewing_slots WHERE id = $1 AND listing_id = $2 FOR UPDATE";

    private const string OthersCountSql = "SELECT count(*) FROM listing_viewing_bookings WHERE slot_id = $1 AND token_hash <> $2";

    private const string AlreadyBookedSql = "SELECT EXISTS (SELECT 1 FROM listing_viewing_bookings WHERE slot_id = $1 AND token_hash = $2)";

    private const string CancelSql = """
        DELETE FROM listing_viewing_bookings b USING listing_viewing_slots s
        WHERE b.slot_id = s.id AND s.listing_id = $1 AND b.token_hash = $2
        """;

    private const string InsertBookingSql = "INSERT INTO listing_viewing_bookings (slot_id, token_hash, created_at) VALUES ($1, $2, $3)";

    public async Task<IReadOnlyList<ViewingSlot>> GetSlotsAsync(Guid listingId, string? tokenHash, DateTimeOffset from, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SlotsSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)tokenHash ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { Value = from });
        command.Parameters.Add(new NpgsqlParameter { Value = AnonymousAttendee });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        List<ViewingSlot> slots = [];
        while (await reader.ReadAsync(ct))
        {
            slots.Add(new ViewingSlot(
                reader.GetInt64(0),
                reader.GetFieldValue<DateTimeOffset>(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetInt32(3),
                Convert.ToInt32(reader.GetInt64(4)),
                reader.GetBoolean(5),
                reader.GetFieldValue<string[]>(6)));
        }

        return slots;
    }

    public async Task<long> AddSlotAsync(Guid listingId, DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(AddSql);
        foreach (object value in new object[] { listingId, startsAt, endsAt, capacity })
        {
            command.Parameters.Add(new NpgsqlParameter { Value = value });
        }

        return (long)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task<bool> RemoveEmptySlotAsync(Guid listingId, long slotId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(RemoveEmptySql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = slotId });
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<BookingOutcome> BookAsync(Guid listingId, long slotId, string tokenHash, DateTimeOffset now, CancellationToken ct)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        BookingOutcome outcome = await TryBookAsync(connection, transaction, listingId, slotId, tokenHash, now, ct);
        await transaction.CommitAsync(ct);
        return outcome;
    }

    public async Task<bool> CancelAsync(Guid listingId, string tokenHash, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(CancelSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = tokenHash });
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private static async Task<BookingOutcome> TryBookAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid listingId,
        long slotId,
        string tokenHash,
        DateTimeOffset now,
        CancellationToken ct)
    {
        (int Capacity, DateTimeOffset EndsAt)? slot = await LockSlotAsync(connection, transaction, listingId, slotId, ct);
        if (slot is null || slot.Value.EndsAt <= now)
        {
            return BookingOutcome.NotFound;
        }

        if (await ScalarAsync<bool>(connection, transaction, AlreadyBookedSql, [slotId, tokenHash], ct))
        {
            return BookingOutcome.Booked;
        }

        if (await ScalarAsync<long>(connection, transaction, OthersCountSql, [slotId, tokenHash], ct) >= slot.Value.Capacity)
        {
            return BookingOutcome.Full;
        }

        await ExecuteAsync(connection, transaction, CancelSql, [listingId, tokenHash], ct);
        await ExecuteAsync(connection, transaction, InsertBookingSql, [slotId, tokenHash, now], ct);
        return BookingOutcome.Booked;
    }

    private static async Task<(int Capacity, DateTimeOffset EndsAt)?> LockSlotAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid listingId,
        long slotId,
        CancellationToken ct)
    {
        await using NpgsqlCommand command = new(LockSlotSql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter { Value = slotId });
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? (reader.GetInt32(0), reader.GetFieldValue<DateTimeOffset>(1)) : null;
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, object[] values, CancellationToken ct)
    {
        await using NpgsqlCommand command = new(sql, connection, transaction);
        foreach (object value in values)
        {
            command.Parameters.Add(new NpgsqlParameter { Value = value });
        }

        return (T)(await command.ExecuteScalarAsync(ct))!;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, object[] values, CancellationToken ct)
    {
        await using NpgsqlCommand command = new(sql, connection, transaction);
        foreach (object value in values)
        {
            command.Parameters.Add(new NpgsqlParameter { Value = value });
        }

        await command.ExecuteNonQueryAsync(ct);
    }
}
