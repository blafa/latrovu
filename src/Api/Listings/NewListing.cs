namespace Latrovu.Api.Listings;

public sealed record NewListing(string SourceEmail, string? Subject, string? FileName, string? Data);
