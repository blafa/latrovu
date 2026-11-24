using System.Security.Cryptography;
using System.Text;
using Latrovu.BuildingBlocks.Ai;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Latrovu.BuildingBlocks.Infrastructure.Ai;

public sealed class CachingLlmClient(
    InfomaniakLlmClient inner,
    NpgsqlDataSource dataSource,
    IOptions<AiOptions> options,
    ILogger<CachingLlmClient> logger) : ILlmClient
{
    private const string SelectSql = "SELECT response FROM llm_response_cache WHERE cache_key = $1";

    private const string InsertSql = """
        INSERT INTO llm_response_cache (cache_key, model, response) VALUES ($1, $2, $3)
        ON CONFLICT (cache_key) DO NOTHING
        """;

    private readonly string model = options.Value.Model;

    public async Task<string?> CompleteAsync(
        string systemPrompt,
        string userContent,
        JsonResponseSchema responseSchema,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(responseSchema);

        string key = CacheKey(systemPrompt, userContent, responseSchema);
        string? cached = await ReadAsync(key, ct);
        if (cached is not null)
        {
            logger.LogInformation("LLM answer for schema {Schema} served from cache", responseSchema.Name);
            return cached;
        }

        string? answer = await inner.CompleteAsync(systemPrompt, userContent, responseSchema, ct);
        if (answer is not null)
        {
            await WriteAsync(key, answer, ct);
        }

        return answer;
    }

    private string CacheKey(string systemPrompt, string userContent, JsonResponseSchema schema)
    {
        string material = string.Join('\n', model, systemPrompt, schema.Name, schema.Schema, userContent);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private async Task<string?> ReadAsync(string key, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SelectSql);
        command.Parameters.Add(new NpgsqlParameter { Value = key });
        return await command.ExecuteScalarAsync(ct) as string;
    }

    private async Task WriteAsync(string key, string answer, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = key });
        command.Parameters.Add(new NpgsqlParameter { Value = model });
        command.Parameters.Add(new NpgsqlParameter { Value = answer });
        await command.ExecuteNonQueryAsync(ct);
    }
}
