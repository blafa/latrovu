namespace Latrovu.Listings;

public sealed record DocumentClassification(DocumentType Type, string? Reason)
{
    public bool IsListing => Type is DocumentType.SingleListing or DocumentType.MultiUnitListing;
}
