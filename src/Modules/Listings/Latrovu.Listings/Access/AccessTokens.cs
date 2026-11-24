using Latrovu.BuildingBlocks.Security;

namespace Latrovu.Listings.Access;

public static class AccessTokens
{
    public const int TokenBytes = SecureTokens.TokenBytes;

    public static string Create() => SecureTokens.Create();

    public static string Hash(string token) => SecureTokens.Hash(token);
}
