using System.Security.Cryptography;
using System.Text;

namespace Latrovu.Listings;

public static class AccessTokens
{
    public const int TokenBytes = 32;

    public static string Create() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
