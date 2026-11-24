using System.Text.Json.Nodes;
using Latrovu.BuildingBlocks.Geo;
using Latrovu.Core.Listings;
using Latrovu.Listings.Matching;

namespace Latrovu.Listings.Tests.Matching;

public sealed class ListingMatchFactsTests
{
    private static readonly JsonObject Data = JsonNode.Parse("""
        {
          "offerType": "rent",
          "location": { "street": "Elsterweg", "houseNumber": "1", "zip": 3603, "city": "Thun" },
          "size": { "rooms": 4.5, "livingAreaM2": 117.5 },
          "rent": { "netChf": 2100, "grossChf": 2350 }
        }
        """)!.AsObject();

    [Fact]
    public void AddressOf_ReadsStreetNumberZipAndCity()
    {
        PostalAddress address = ListingMatchFacts.AddressOf(Data);

        Assert.Equal("Elsterweg 1 3603 Thun", address.FullText);
    }

    [Fact]
    public void Announce_UsesGrossRentAndPoint()
    {
        GeoPoint point = new(2614000, 1178000, GeoPoint.AddressPrecision);

        ListingReadyForMatching announcement = ListingMatchFacts.Announce(Guid.NewGuid(), Data, point);

        Assert.Equal(2350m, announcement.PriceChf);
        Assert.Equal(4.5m, announcement.Rooms);
        Assert.Equal("rent", announcement.OfferType);
        Assert.Equal(2614000, announcement.East);
    }
}
