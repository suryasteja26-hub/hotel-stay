using HotelStay.Api.Domain;
using HotelStay.Api.Providers;
using HotelStay.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelStay.Tests;

public class HotelSearchServiceTests
{
    // Stub returning a single fixed offer, used to verify aggregation/ordering
    // without coupling to real provider data.
    private sealed class StubProvider : IHotelProvider
    {
        private readonly decimal _price;
        private readonly string _hotelName;

        public StubProvider(string providerId, decimal price, string? hotelName = null)
        {
            ProviderId = providerId;
            _price = price;
            _hotelName = hotelName ?? $"{providerId} Hotel";
        }

        public string ProviderId { get; }

        public Task<IReadOnlyList<HotelOffer>> SearchAsync(
            SearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            var offer = new HotelOffer(
                ProviderId: ProviderId,
                HotelId: $"{ProviderId}-1",
                HotelName: _hotelName,
                City: criteria.Destination,
                RoomType: RoomType.Standard,
                PricePerNight: _price,
                Currency: "GBP",
                AvailableRooms: null,
                Description: null,
                CancellationPolicy: CancellationPolicy.Flexible24Hours,
                Amenities: Array.Empty<string>(),
                StarRating: null);

            return Task.FromResult<IReadOnlyList<HotelOffer>>(new[] { offer });
        }
    }

    // Always throws — used to prove a failing provider does not fail the search.
    private sealed class ThrowingProvider : IHotelProvider
    {
        public string ProviderId => "Broken";

        public Task<IReadOnlyList<HotelOffer>> SearchAsync(
            SearchCriteria criteria, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("provider is down");
    }

    private static HotelSearchService CreateService(params IHotelProvider[] providers) =>
        new(providers, NullLogger<HotelSearchService>.Instance);

    private static SearchCriteria AnyCriteria() =>
        new("London", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 13), null);

    [Fact]
    public async Task Queries_all_injected_providers_and_merges_results()
    {
        var service = CreateService(
            new StubProvider("Alpha", 100m),
            new StubProvider("Beta", 200m));

        var response = await service.SearchAsync(AnyCriteria());

        Assert.Equal(2, response.Results.Count);
        Assert.Contains(response.Results, o => o.ProviderId == "Alpha");
        Assert.Contains(response.Results, o => o.ProviderId == "Beta");
    }

    [Fact]
    public async Task Sorts_by_pricePerNight_then_hotelName()
    {
        var service = CreateService(
            new StubProvider("Expensive", 300m, "Zeta Grand"),
            new StubProvider("CheapB", 75m, "Beta Inn"),
            new StubProvider("CheapA", 75m, "Alpha Inn"),
            new StubProvider("Mid", 150m, "Mid Lodge"));

        var response = await service.SearchAsync(AnyCriteria());

        // Price ascending, ties broken by hotel name ascending.
        Assert.Equal(
            new[] { "Alpha Inn", "Beta Inn", "Mid Lodge", "Zeta Grand" },
            response.Results.Select(o => o.HotelName));
    }

    [Fact]
    public async Task One_provider_throwing_returns_partial_results()
    {
        var service = CreateService(
            new StubProvider("Healthy", 120m),
            new ThrowingProvider());

        var response = await service.SearchAsync(AnyCriteria());

        Assert.Single(response.Results);
        Assert.Equal("Healthy", response.Results[0].ProviderId);
    }

    [Fact]
    public async Task Response_echoes_query_and_derives_nights()
    {
        var service = CreateService(new StubProvider("Alpha", 100m));
        var criteria = new SearchCriteria("Paris", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 14), null);

        var response = await service.SearchAsync(criteria);

        Assert.Equal("Paris", response.Destination);
        Assert.Equal(new DateOnly(2026, 7, 10), response.CheckIn);
        Assert.Equal(new DateOnly(2026, 7, 14), response.CheckOut);
        Assert.Equal(4, response.Nights);
    }

    [Fact]
    public async Task Unknown_destination_returns_empty_results_not_error()
    {
        // Real providers; "Atlantis" is in no inventory.
        var service = CreateService(new PremierStaysProvider(), new BudgetNestsProvider());
        var criteria = new SearchCriteria("Atlantis", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 13), null);

        var response = await service.SearchAsync(criteria);

        Assert.Empty(response.Results);
    }
}
