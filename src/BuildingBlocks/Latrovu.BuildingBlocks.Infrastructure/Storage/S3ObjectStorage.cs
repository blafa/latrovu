using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Latrovu.BuildingBlocks.Storage;
using Microsoft.Extensions.Options;

namespace Latrovu.BuildingBlocks.Infrastructure.Storage;

public sealed class S3ObjectStorage(
    HttpClient http,
    IOptions<StorageOptions> options,
    TimeProvider timeProvider,
    ILogger<S3ObjectStorage> logger) : IObjectStorage
{
    private const string AuthorizationHeader = "Authorization";
    private const string AmzDateHeader = "x-amz-date";
    private const string ContentSha256Header = "x-amz-content-sha256";
    private const string FallbackContentType = "application/octet-stream";

    private readonly StorageOptions settings = options.Value;

    public async Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);

        Stopwatch stopwatch = Stopwatch.StartNew();
        using HttpRequestMessage request = CreateSignedRequest(HttpMethod.Put, key, content);
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using HttpResponseMessage response = await http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, key, ct);

        logger.LogDebug("Uploaded {Key} ({Bytes} bytes) in {ElapsedMs} ms", key, content.Length, stopwatch.ElapsedMilliseconds);
    }

    public async Task<StoredObject?> GetAsync(string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Stopwatch stopwatch = Stopwatch.StartNew();
        using HttpRequestMessage request = CreateSignedRequest(HttpMethod.Get, key, []);
        using HttpResponseMessage response = await http.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("Object {Key} not found", key);
            return null;
        }

        await EnsureSuccessAsync(response, key, ct);
        byte[] content = await response.Content.ReadAsByteArrayAsync(ct);
        string contentType = response.Content.Headers.ContentType?.MediaType ?? FallbackContentType;
        logger.LogDebug("Downloaded {Key} ({Bytes} bytes) in {ElapsedMs} ms", key, content.Length, stopwatch.ElapsedMilliseconds);
        return new StoredObject(content, contentType);
    }

    private HttpRequestMessage CreateSignedRequest(HttpMethod method, string key, byte[] payload)
    {
        Uri uri = new($"{settings.Endpoint.TrimEnd('/')}/{settings.Bucket}/{key}");
        SignedHeaders signed = SigV4Signer.Sign(
            method.Method,
            uri,
            payload,
            settings.Region,
            settings.AccessKey,
            settings.SecretKey,
            timeProvider.GetUtcNow());

        HttpRequestMessage request = new(method, uri);
        request.Headers.TryAddWithoutValidation(AuthorizationHeader, signed.Authorization);
        request.Headers.Add(AmzDateHeader, signed.AmzDate);
        request.Headers.Add(ContentSha256Header, signed.ContentSha256);
        return request;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string key, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(ct);
        logger.LogError("Storage request for {Key} failed with {StatusCode}: {Body}", key, (int)response.StatusCode, body);
        throw new HttpRequestException($"Storage request failed with {(int)response.StatusCode}", null, response.StatusCode);
    }
}
