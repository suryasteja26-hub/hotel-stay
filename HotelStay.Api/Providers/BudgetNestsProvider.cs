using HotelStay.Api.Domain;

namespace HotelStay.Api.Providers;

/// <summary>
/// Deterministic stub provider with minimal-detail inventory (rate and policy
/// only). Source data mimics a snake_case upstream feed; rows flagged
/// <c>available = false</c> are filtered out. Only hotels in the requested city
/// are returned. Does not operate in every city (e.g. New York, Tokyo) — useful
/// for exercising partial / single-provider results.
/// </summary>
public sealed class BudgetNestsProvider : IHotelProvider
{
    public string ProviderId => "BudgetNests";

    // snake_case-style raw feed, as an upstream API would expose it.
    private sealed record BudgetNestsHotel(
        string hotel_code,
        string hotel_name,
        string city,
        BudgetNestsOffer[] offers);

    private sealed record BudgetNestsOffer(
        string room_type,
        decimal price_per_night,
        string policy,
        bool available);

    // Fixed, deterministic inventory. Note the unavailable London Suite row and
    // the absence of New York / Tokyo (only PremierStays serves those).
    private static readonly IReadOnlyList<BudgetNestsHotel> Source = new[]
    {
        new BudgetNestsHotel("BN-LON-007", "Budget Nest Camden", "London", new[]
        {
            new BudgetNestsOffer("standard", 72.50m, "Flexible", true),
            new BudgetNestsOffer("deluxe", 95.00m, "NonRefundable", true),
            new BudgetNestsOffer("suite", 140.00m, "Flexible", false),
        }),
        new BudgetNestsHotel("BN-MAN-003", "Budget Nest Manchester", "Manchester", new[]
        {
            new BudgetNestsOffer("standard", 60.00m, "Flexible", true),
            new BudgetNestsOffer("deluxe", 88.00m, "Flexible", true),
        }),
        new BudgetNestsHotel("BN-PAR-002", "Budget Nest Paris", "Paris", new[]
        {
            new BudgetNestsOffer("standard", 88.00m, "Flexible", true),
            new BudgetNestsOffer("suite", 165.00m, "NonRefundable", false),
        }),
    };

    public Task<IReadOnlyList<HotelOffer>> SearchAsync(
        SearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var offers = Source
            .Where(hotel => string.Equals(hotel.city, criteria.Destination, StringComparison.OrdinalIgnoreCase))
            .SelectMany(hotel => hotel.offers
                .Where(offer => offer.available)
                .Select(offer => Normalize(hotel, offer)))
            .Where(offer => criteria.RoomType is null || offer.RoomType == criteria.RoomType)
            .ToList();

        return Task.FromResult<IReadOnlyList<HotelOffer>>(offers);
    }

    private HotelOffer Normalize(BudgetNestsHotel hotel, BudgetNestsOffer offer) =>
        new(
            ProviderId: ProviderId,
            HotelId: hotel.hotel_code,
            HotelName: hotel.hotel_name,
            City: hotel.city,
            RoomType: ParseRoomType(offer.room_type),
            PricePerNight: offer.price_per_night,
            Currency: "GBP",
            AvailableRooms: null,   // minimal provider does not supply room counts
            Description: null,      // minimal provider supplies no description
            CancellationPolicy: MapCancellationPolicy(offer.policy),
            Amenities: Array.Empty<string>(),
            StarRating: null);

    private static RoomType ParseRoomType(string value) => value.ToLowerInvariant() switch
    {
        "standard" => RoomType.Standard,
        "deluxe" => RoomType.Deluxe,
        "suite" => RoomType.Suite,
        _ => throw new InvalidOperationException($"Unknown room type '{value}'.")
    };

    private static CancellationPolicy MapCancellationPolicy(string value) => value switch
    {
        "Flexible" => CancellationPolicy.Flexible24Hours,
        "NonRefundable" => CancellationPolicy.NonRefundable,
        _ => throw new InvalidOperationException($"Unknown cancellation policy '{value}'.")
    };
}
