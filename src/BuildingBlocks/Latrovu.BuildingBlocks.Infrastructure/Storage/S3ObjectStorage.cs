using Latrovu.BuildingBlocks.Storage;
using System.Diagnostics;
using System.Net.Http.Headers;
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

    private readonly StorageOptions settings = options.Value;

    public async Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);

        Stopwatch stopwatch = Stopwatch.StartNew();
        using HttpRequestMessage request = CreatePutRequest(key, content, contentType);
        using HttpResponseMessage response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Upload of {Key} failed with {StatusCode}: {Body}", key, (int)response.StatusCode, body);
            throw new HttpRequestException($"Upload failed with {(int)response.StatusCode}", null, response.StatusCode);
        }

        logger.LogDebug("Uploaded {Key} ({Bytes} bytes) in {ElapsedMs} ms", key, content.Length, stopwatch.ElapsedMilliseconds);
    }

    private HttpRequestMessage CreatePutRequest(string key, byte[] content, string contentType)
    {
        Uri uri = new($"{settings.Endpoint.TrimEnd('/')}/{settings.Bucket}/{key}");
        SignedHeaders signed = SigV4Signer.Sign(
            HttpMethod.Put.Method,
            uri,
            content,
            settings.Region,
            settings.AccessKey,
            settings.SecretKey,
            timeProvider.GetUtcNow());

        HttpRequestMessage request = new(HttpMethod.Put, uri) { Content = new ByteArrayContent(content) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Headers.TryAddWithoutValidation(AuthorizationHeader, signed.Authorization);
        request.Headers.Add(AmzDateHeader, signed.AmzDate);
        request.Headers.Add(ContentSha256Header, signed.ContentSha256);
        return request;
    }
}
