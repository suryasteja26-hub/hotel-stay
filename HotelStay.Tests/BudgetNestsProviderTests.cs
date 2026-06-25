using HotelStay.Api.Domain;
using HotelStay.Api.Providers;

namespace HotelStay.Tests;

public class BudgetNestsProviderTests
{
    private static SearchCriteria Criteria(string city = "London", RoomType? roomType = null) =>
        new(city, new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 13), roomType);

    [Fact]
    public async Task Maps_minimal_detail_offer_with_nulls_for_missing_fields()
    {
        var provider = new BudgetNestsProvider();

        var results = await provider.SearchAsync(Criteria("London"));

        var standard = Assert.Single(results, o => o.RoomType == RoomType.Standard);
        Assert.Equal("BudgetNests", standard.ProviderId);
        Assert.Equal("BN-LON-007", standard.HotelId);
        Assert.Equal(72.50m, standard.PricePerNight);
        Assert.Null(standard.AvailableRooms);   // minimal provider supplies no count
        Assert.Null(standard.Description);
        Assert.Empty(standard.Amenities);
        Assert.Null(standard.StarRating);
    }

    [Fact]
    public async Task Filters_out_unavailable_rooms()
    {
        var provider = new BudgetNestsProvider();

        var results = await provider.SearchAsync(Criteria("London"));

        // The London Suite row is flagged unavailable in the source data.
        Assert.DoesNotContain(results, o => o.RoomType == RoomType.Suite);
    }

    [Fact]
    public async Task Returns_empty_for_city_it_does_not_serve()
    {
        var provider = new BudgetNestsProvider();

        // BudgetNests does not operate in New York; PremierStays does.
        var results = await provider.SearchAsync(Criteria("New York"));

        Assert.Empty(results);
    }

    [Fact]
    public async Task Applies_roomType_filter()
    {
        var provider = new BudgetNestsProvider();

        var results = await provider.SearchAsync(Criteria("London", RoomType.Standard));

        Assert.NotEmpty(results);
        Assert.All(results, o => Assert.Equal(RoomType.Standard, o.RoomType));
    }
}
