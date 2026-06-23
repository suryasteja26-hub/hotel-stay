using HotelStay.Api.Domain;
using HotelStay.Api.Providers;
using HotelStay.Api.Services;

namespace HotelStay.Tests;

public class HotelSearchServiceTests
{
    // Minimal stub provider returning a single fixed-price option, used to
    // verify aggregation/ordering without coupling to real provider data.
    private sealed class StubProvider : IHotelProvider
    {
        private readonly decimal _totalPrice;

        public StubProvider(string name, decimal totalPrice)
        {
            Name = name;
            _totalPrice = totalPrice;
        }

        public string Name { get; }

        public Task<IReadOnlyList<HotelRoomOption>> SearchAsync(
            HotelSearchRequest request, CancellationToken cancellationToken)
        {
            var option = new HotelRoomOption(
                Id: $"{Name}-1",
                Provider: Name,
                Destination: request.Destination,
                RoomType: RoomType.Standard,
                PerNightRate: _totalPrice,
                TotalPrice: _totalPrice,
                Nights: 1,
                CancellationPolicy: CancellationPolicy.Flexible24Hours,
                Amenities: Array.Empty<string>(),
                StarRating: 3);

            return Task.FromResult<IReadOnlyList<HotelRoomOption>>(new[] { option });
        }
    }

    private static HotelSearchRequest AnyRequest() =>
        new("London", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 11), null);

    [Fact]
    public async Task Queries_all_injected_providers()
    {
        var providers = new IHotelProvider[]
        {
            new StubProvider("Alpha", 100m),
            new StubProvider("Beta", 200m),
        };
        var service = new HotelSearchService(providers);

        var results = await service.SearchAsync(AnyRequest(), CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, option => option.Provider == "Alpha");
        Assert.Contains(results, option => option.Provider == "Beta");
    }

    [Fact]
    public async Task Sorts_results_by_total_price_ascending()
    {
        var providers = new IHotelProvider[]
        {
            new StubProvider("Expensive", 300m),
            new StubProvider("Cheap", 75m),
            new StubProvider("Mid", 150m),
        };
        var service = new HotelSearchService(providers);

        var results = await service.SearchAsync(AnyRequest(), CancellationToken.None);

        var prices = results.Select(option => option.TotalPrice).ToList();
        Assert.Equal(new[] { 75m, 150m, 300m }, prices);
    }
}
