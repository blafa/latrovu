using Latrovu.Api.Storage;
using Xunit;

namespace Latrovu.Api.Tests;

public sealed class SigV4SignerTests
{
    private const string EmptyPayloadSha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    [Fact]
    public void DeriveSigningKey_MatchesAwsDocumentationExample()
    {
        byte[] key = SigV4Signer.DeriveSigningKey(
            "wJalrXUtnFEMI/K7MDENG+bPxRfiCYEXAMPLEKEY",
            "20120215",
            "us-east-1",
            "iam");

        Assert.Equal("f4780e2d9f65fa895f9c67b32ce1baf0b0d8a43505a000a1a9e090d414db404d", SigV4Signer.Hex(key));
    }

    [Fact]
    public void Sign_ProducesHeadersWithScopeDateAndPayloadHash()
    {
        DateTimeOffset now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        Uri uri = new("https://s3.pub1.infomaniak.cloud/latrovu/listings/test/01.jpg");

        SignedHeaders signed = SigV4Signer.Sign("PUT", uri, [], "us-east-1", "AKID", "SECRET", now);

        Assert.Equal("20260101T120000Z", signed.AmzDate);
        Assert.Equal(EmptyPayloadSha256, signed.ContentSha256);
        Assert.StartsWith(
            "AWS4-HMAC-SHA256 Credential=AKID/20260101/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature=",
            signed.Authorization);
        Assert.Matches("Signature=[0-9a-f]{64}$", signed.Authorization);
    }

    [Fact]
    public void Sign_IsDeterministicForSameInput()
    {
        DateTimeOffset now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        Uri uri = new("https://s3.pub1.infomaniak.cloud/latrovu/a.jpg");

        SignedHeaders first = SigV4Signer.Sign("PUT", uri, [1, 2, 3], "us-east-1", "AKID", "SECRET", now);
        SignedHeaders second = SigV4Signer.Sign("PUT", uri, [1, 2, 3], "us-east-1", "AKID", "SECRET", now);

        Assert.Equal(first.Authorization, second.Authorization);
    }
}
