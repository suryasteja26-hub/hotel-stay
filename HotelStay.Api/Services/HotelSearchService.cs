using HotelStay.Api.Domain;
using HotelStay.Api.Providers;

namespace HotelStay.Api.Services;

/// <summary>
/// Aggregates normalized offers from all registered providers. Tolerates an
/// individual provider failing (logs and omits its results) so one bad provider
/// never fails the whole search. Applies the cross-provider default ordering.
/// </summary>
public sealed class HotelSearchService
{
    private readonly IEnumerable<IHotelProvider> _providers;
    private readonly ILogger<HotelSearchService> _logger;

    public HotelSearchService(
        IEnumerable<IHotelProvider> providers,
        ILogger<HotelSearchService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<SearchResponse> SearchAsync(
        SearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var nights = criteria.CheckOut.DayNumber - criteria.CheckIn.DayNumber;

        var providerResults = await Task.WhenAll(
            _providers.Select(provider => SearchSafelyAsync(provider, criteria, cancellationToken)));

        var offers = providerResults
            .SelectMany(offers => offers)
            .OrderBy(offer => offer.PricePerNight)
            .ThenBy(offer => offer.HotelName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SearchResponse(criteria.Destination, criteria.CheckIn, criteria.CheckOut, nights, offers);
    }

    // A single provider throwing must not fail the aggregate search; we log and
    // contribute an empty result set so the others still surface (resilience).
    private async Task<IReadOnlyList<HotelOffer>> SearchSafelyAsync(
        IHotelProvider provider,
        SearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.SearchAsync(criteria, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider {ProviderId} failed; returning partial results.", provider.ProviderId);
            return Array.Empty<HotelOffer>();
        }
    }
}
