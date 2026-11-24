using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Review;

public sealed class ListingReviewStore(NpgsqlDataSource dataSource, IOutbox outbox) : IListingReviewStore
{
    private const string DraftStatus = "draft";

    private const string SelectListingSql = """
        SELECT id, status, source_email, file_name, COALESCE(data, '{}'::jsonb)::text, COALESCE(field_scores, '{}'::jsonb)::text,
               COALESCE(field_evidence, '{}'::jsonb)::text, publish_contact
        FROM listings WHERE id = $1
        """;

    private const string SelectImagesSql =
        "SELECT position, storage_key, content_type FROM listing_images WHERE listing_id = $1 ORDER BY position";

    private const string SaveSql = "UPDATE listings SET data = $2::jsonb, field_scores = $3::jsonb WHERE id = $1";

    private const string PublishContactSql = "UPDATE listings SET publish_contact = $2 WHERE id = $1";

    private const string ChangeStatusSql = "UPDATE listings SET status = $2 WHERE id = $1 AND status = ANY($3)";

    public async Task<ListingReview?> GetAsync(Guid listingId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SelectListingSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        ListingReview review = new(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            await reader.IsDBNullAsync(3, ct) ? null : reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetBoolean(7),
            []);
        return review with { Images = await GetImagesAsync(listingId, ct) };
    }

    public async Task SaveAsync(Guid listingId, string dataJson, string fieldScoresJson, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SaveSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = dataJson });
        command.Parameters.Add(new NpgsqlParameter { Value = fieldScoresJson });
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task SetPublishContactAsync(Guid listingId, bool publishContact, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(PublishContactSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = publishContact });
        await command.ExecuteNonQueryAsync(ct);
    }

    public Task<bool> ApproveAsync(Guid listingId, CancellationToken ct) =>
        ChangeStatusAsync(listingId, ListingReview.ApprovedStatus, [DraftStatus, ListingReview.WithdrawnStatus], new ListingApproved(listingId), ct);

    public Task<bool> WithdrawAsync(Guid listingId, CancellationToken ct) =>
        ChangeStatusAsync(listingId, ListingReview.WithdrawnStatus, [ListingReview.ApprovedStatus], new ListingWithdrawn(listingId), ct);

    private async Task<bool> ChangeStatusAsync(
        Guid listingId,
        string newStatus,
        string[] allowedCurrentStatuses,
        object statusEvent,
        CancellationToken ct)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        await using NpgsqlCommand command = new(ChangeStatusSql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = newStatus });
        command.Parameters.Add(new NpgsqlParameter { Value = allowedCurrentStatuses });
        bool changed = await command.ExecuteNonQueryAsync(ct) > 0;
        if (changed)
        {
            await outbox.AddAsync([statusEvent], transaction, ct);
        }

        await transaction.CommitAsync(ct);
        return changed;
    }

    private async Task<List<ListingImageRef>> GetImagesAsync(Guid listingId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SelectImagesSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        List<ListingImageRef> images = [];
        while (await reader.ReadAsync(ct))
        {
            images.Add(new ListingImageRef(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        }

        return images;
    }
}
