using HotelStay.Api.Domain;
using HotelStay.Api.Providers;

namespace HotelStay.Tests;

public class BudgetNestsProviderTests
{
    private static HotelSearchRequest Request(RoomType? roomType = null, int nights = 3) =>
        new(
            Destination: "London",
            CheckIn: new DateOnly(2026, 7, 10),
            CheckOut: new DateOnly(2026, 7, 10).AddDays(nights),
            RoomType: roomType);

    [Fact]
    public async Task Filters_out_unavailable_rooms()
    {
        var provider = new BudgetNestsProvider();

        var results = await provider.SearchAsync(Request(), CancellationToken.None);

        // The Suite row is flagged unavailable in the source data.
        Assert.DoesNotContain(results, option => option.RoomType == RoomType.Suite);
        Assert.All(results, option => Assert.NotEqual(RoomType.Suite, option.RoomType));
    }

    [Fact]
    public async Task Calculates_total_price_as_rate_times_nights()
    {
        var provider = new BudgetNestsProvider();

        var results = await provider.SearchAsync(Request(nights: 4), CancellationToken.None);

        Assert.NotEmpty(results);
        Assert.All(results, option =>
        {
            Assert.Equal(4, option.Nights);
            Assert.Equal(option.PerNightRate * 4, option.TotalPrice);
        });
    }

    [Fact]
    public async Task Applies_roomType_filter()
    {
        var provider = new BudgetNestsProvider();

        var results = await provider.SearchAsync(Request(RoomType.Standard), CancellationToken.None);

        Assert.NotEmpty(results);
        Assert.All(results, option => Assert.Equal(RoomType.Standard, option.RoomType));
    }
}
