using Npgsql;

namespace Latrovu.Listings.Infrastructure.Images;

public sealed class ListingImageStore(NpgsqlDataSource dataSource) : IListingImageStore
{
    private const string InsertSql =
        "INSERT INTO listing_images (listing_id, position, storage_key, content_type) VALUES ($1, $2, $3, $4)";

    public async Task AddAsync(Guid listingId, int position, string storageKey, string contentType, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = position });
        command.Parameters.Add(new NpgsqlParameter { Value = storageKey });
        command.Parameters.Add(new NpgsqlParameter { Value = contentType });
        await command.ExecuteNonQueryAsync(ct);
    }
}
