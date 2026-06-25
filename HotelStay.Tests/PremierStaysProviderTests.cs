using HotelStay.Api.Domain;
using HotelStay.Api.Providers;

namespace HotelStay.Tests;

public class PremierStaysProviderTests
{
    private static SearchCriteria Criteria(string city = "London", RoomType? roomType = null) =>
        new(city, new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 13), roomType);

    [Fact]
    public async Task Maps_full_detail_offer_for_known_city()
    {
        var provider = new PremierStaysProvider();

        var results = await provider.SearchAsync(Criteria("London"));

        var deluxe = Assert.Single(results, o => o.RoomType == RoomType.Deluxe);
        Assert.Equal("PremierStays", deluxe.ProviderId);
        Assert.Equal("PS-LON-001", deluxe.HotelId);
        Assert.Equal("Premier Thames View", deluxe.HotelName);
        Assert.Equal("London", deluxe.City);
        Assert.Equal(189.00m, deluxe.PricePerNight);
        Assert.Equal("GBP", deluxe.Currency);
        Assert.NotNull(deluxe.AvailableRooms);          // full provider supplies counts
        Assert.False(string.IsNullOrWhiteSpace(deluxe.Description));
        Assert.NotEmpty(deluxe.Amenities);
        Assert.NotNull(deluxe.StarRating);
    }

    [Fact]
    public async Task Returns_empty_for_unknown_city()
    {
        var provider = new PremierStaysProvider();

        var results = await provider.SearchAsync(Criteria("Atlantis"));

        Assert.Empty(results);
    }

    [Fact]
    public async Task Applies_roomType_filter()
    {
        var provider = new PremierStaysProvider();

        var results = await provider.SearchAsync(Criteria("London", RoomType.Suite));

        Assert.NotEmpty(results);
        Assert.All(results, o => Assert.Equal(RoomType.Suite, o.RoomType));
    }

    [Theory]
    [InlineData("london")]
    [InlineData("LONDON")]
    public async Task Matches_destination_case_insensitively(string city)
    {
        var provider = new PremierStaysProvider();

        var results = await provider.SearchAsync(Criteria(city));

        Assert.NotEmpty(results);
    }
}
