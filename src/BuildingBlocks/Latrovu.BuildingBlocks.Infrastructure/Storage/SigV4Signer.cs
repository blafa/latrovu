using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Latrovu.BuildingBlocks.Infrastructure.Storage;

// AWS Signature Version 4, see https://docs.aws.amazon.com/AmazonS3/latest/API/sig-v4-header-based-auth.html
public static class SigV4Signer
{
    public const string Algorithm = "AWS4-HMAC-SHA256";
    public const string SignedHeaderNames = "host;x-amz-content-sha256;x-amz-date";
    private const string Service = "s3";
    private const string ScopeTerminator = "aws4_request";
    private const string SecretPrefix = "AWS4";
    private const string AmzDateFormat = "yyyyMMdd'T'HHmmss'Z'";
    private const string DateStampFormat = "yyyyMMdd";
    private const string HexFormat = "x2";

    public static SignedHeaders Sign(
        string method,
        Uri uri,
        byte[] payload,
        string region,
        string accessKey,
        string secretKey,
        DateTimeOffset now)
    {
        DateTimeOffset utc = now.ToUniversalTime();
        string amzDate = utc.ToString(AmzDateFormat, CultureInfo.InvariantCulture);
        string dateStamp = utc.ToString(DateStampFormat, CultureInfo.InvariantCulture);
        string payloadHash = Hex(SHA256.HashData(payload));
        string scope = $"{dateStamp}/{region}/{Service}/{ScopeTerminator}";

        string canonicalRequest = CanonicalRequest(method, uri, payloadHash, amzDate);
        string stringToSign = $"{Algorithm}\n{amzDate}\n{scope}\n{Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))}";
        byte[] signingKey = DeriveSigningKey(secretKey, dateStamp, region, Service);
        string signature = Hex(HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        string authorization = $"{Algorithm} Credential={accessKey}/{scope}, SignedHeaders={SignedHeaderNames}, Signature={signature}";
        return new SignedHeaders(authorization, amzDate, payloadHash);
    }

    public static byte[] DeriveSigningKey(string secretKey, string dateStamp, string region, string service)
    {
        byte[] dateKey = Hmac(Encoding.UTF8.GetBytes(SecretPrefix + secretKey), dateStamp);
        byte[] regionKey = Hmac(dateKey, region);
        byte[] serviceKey = Hmac(regionKey, service);
        return Hmac(serviceKey, ScopeTerminator);
    }

    public static string Hex(byte[] bytes)
    {
        StringBuilder hex = new(bytes.Length * 2);
        foreach (byte value in bytes)
        {
            hex.Append(value.ToString(HexFormat, CultureInfo.InvariantCulture));
        }

        return hex.ToString();
    }

    private static string CanonicalRequest(string method, Uri uri, string payloadHash, string amzDate)
    {
        string host = uri.IsDefaultPort ? uri.Host : uri.Authority;
        string canonicalHeaders = $"host:{host}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n";
        return $"{method}\n{uri.AbsolutePath}\n{uri.Query.TrimStart('?')}\n{canonicalHeaders}\n{SignedHeaderNames}\n{payloadHash}";
    }

    private static byte[] Hmac(byte[] key, string data) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));
}
