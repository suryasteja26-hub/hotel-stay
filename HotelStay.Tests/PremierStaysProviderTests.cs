using HotelStay.Api.Domain;
using HotelStay.Api.Providers;

namespace HotelStay.Tests;

public class PremierStaysProviderTests
{
    private static HotelSearchRequest Request(RoomType? roomType = null, int nights = 3) =>
        new(
            Destination: "Paris",
            CheckIn: new DateOnly(2026, 7, 10),
            CheckOut: new DateOnly(2026, 7, 10).AddDays(nights),
            RoomType: roomType);

    [Fact]
    public async Task Returns_normalized_rooms_for_all_room_types()
    {
        var provider = new PremierStaysProvider();

        var results = await provider.SearchAsync(Request(), CancellationToken.None);

        Assert.Contains(results, option => option.RoomType == RoomType.Standard);
        Assert.Contains(results, option => option.RoomType == RoomType.Deluxe);
        Assert.Contains(results, option => option.RoomType == RoomType.Suite);
        Assert.All(results, option =>
        {
            Assert.Equal("PremierStays", option.Provider);
            Assert.Equal("Paris", option.Destination);
            Assert.True(option.PerNightRate > 0);
        });
    }

    [Fact]
    public async Task Calculates_total_price_as_rate_times_nights()
    {
        var provider = new PremierStaysProvider();

        var results = await provider.SearchAsync(Request(nights: 5), CancellationToken.None);

        Assert.NotEmpty(results);
        Assert.All(results, option =>
        {
            Assert.Equal(5, option.Nights);
            Assert.Equal(option.PerNightRate * 5, option.TotalPrice);
        });
    }
}
