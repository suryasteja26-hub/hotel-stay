using HotelStay.Api.Domain;
using HotelStay.Api.Providers;

namespace HotelStay.Api.Services;

public sealed class HotelSearchService
{
    private readonly IEnumerable<IHotelProvider> _providers;

    public HotelSearchService(IEnumerable<IHotelProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<HotelRoomOption>> SearchAsync(
        HotelSearchRequest request,
        CancellationToken cancellationToken)
    {
        var searches = _providers.Select(provider => provider.SearchAsync(request, cancellationToken));
        var providerResults = await Task.WhenAll(searches);

        return providerResults
            .SelectMany(options => options)
            .OrderBy(option => option.TotalPrice)
            .ToList();
    }
}
