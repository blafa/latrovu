using Npgsql;

namespace Latrovu.Listings.Infrastructure;

public sealed class ListingReviewStore(NpgsqlDataSource dataSource) : IListingReviewStore
{
    private const string DraftStatus = "draft";
    private const string ApprovedStatus = "approved";

    private const string SelectListingSql = """
        SELECT id, status, source_email, file_name, COALESCE(data, '{}'::jsonb)::text, COALESCE(field_scores, '{}'::jsonb)::text
        FROM listings WHERE id = $1
        """;

    private const string SelectImagesSql =
        "SELECT position, storage_key, content_type FROM listing_images WHERE listing_id = $1 ORDER BY position";

    private const string SaveSql = "UPDATE listings SET data = $2::jsonb, field_scores = $3::jsonb WHERE id = $1";

    private const string ApproveSql = "UPDATE listings SET status = $2 WHERE id = $1 AND status = $3";

    public async Task<ListingReview?> GetAsync(Guid listingId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SelectListingSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        string? fileName = await reader.IsDBNullAsync(3, ct) ? null : reader.GetString(3);
        ListingReview review = new(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            fileName,
            reader.GetString(4),
            reader.GetString(5),
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

    public async Task<bool> ApproveAsync(Guid listingId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(ApproveSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = ApprovedStatus });
        command.Parameters.Add(new NpgsqlParameter { Value = DraftStatus });
        return await command.ExecuteNonQueryAsync(ct) > 0;
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
