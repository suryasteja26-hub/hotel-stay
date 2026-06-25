using HotelStay.Api.Domain;

namespace HotelStay.Api.Providers;

/// <summary>
/// One source of hotel availability. Each implementation owns its raw model and
/// is responsible for normalizing into <see cref="HotelOffer"/>, dropping
/// unavailable rows, matching the destination, and applying the room-type filter.
/// The aggregator depends only on this abstraction.
/// </summary>
public interface IHotelProvider
{
    /// <summary>Stable, unique identifier surfaced in offers/reservations, e.g. "PremierStays".</summary>
    string ProviderId { get; }

    /// <summary>
    /// Returns normalized, already-filtered offers for the criteria.
    /// Must return an empty sequence (never throw) when there are no results,
    /// including for unknown destinations.
    /// </summary>
    Task<IReadOnlyList<HotelOffer>> SearchAsync(
        SearchCriteria criteria,
        CancellationToken cancellationToken = default);
}
