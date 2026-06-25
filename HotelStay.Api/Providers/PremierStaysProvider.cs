using HotelStay.Api.Domain;

namespace HotelStay.Api.Providers;

/// <summary>
/// Deterministic stub provider with full-detail inventory (rate, cancellation
/// policy, amenities, star rating). Source data mimics a PascalCase upstream
/// feed and is always available. Only hotels in the requested city are returned.
/// </summary>
public sealed class PremierStaysProvider : IHotelProvider
{
    public string ProviderId => "PremierStays";

    // PascalCase-style raw feed, as an upstream API would expose it.
    private sealed record PremierStaysHotel(
        string HotelCode,
        string HotelName,
        string City,
        PremierStaysRoom[] Rooms);

    private sealed record PremierStaysRoom(
        string RoomType,
        decimal NightlyRate,
        int RoomsLeft,
        string CancellationPolicy,
        string[] Amenities,
        int StarRating,
        string Description);

    private static readonly string[] StandardAmenities = { "WiFi", "Air Conditioning", "Breakfast" };
    private static readonly string[] DeluxeAmenities = { "WiFi", "Air Conditioning", "Breakfast", "City View", "Minibar" };
    private static readonly string[] SuiteAmenities = { "WiFi", "Air Conditioning", "Breakfast", "Lounge Access", "Minibar", "Spa" };

    // Fixed, deterministic inventory keyed implicitly by City. Note coverage:
    // London/Manchester (domestic) and Paris/New York/Tokyo (international).
    private static readonly IReadOnlyList<PremierStaysHotel> Source = new[]
    {
        new PremierStaysHotel("PS-LON-001", "Premier Thames View", "London", new[]
        {
            new PremierStaysRoom("Standard", 120.00m, 6, "FreeCancellation", StandardAmenities, 4, "Queen bed, city view, breakfast included"),
            new PremierStaysRoom("Deluxe", 189.00m, 4, "FreeCancellation", DeluxeAmenities, 5, "King bed, river view, breakfast included"),
            new PremierStaysRoom("Suite", 320.00m, 2, "NonRefundable", SuiteAmenities, 5, "Two-room suite, river view, lounge access"),
        }),
        new PremierStaysHotel("PS-MAN-001", "Premier Manchester Central", "Manchester", new[]
        {
            new PremierStaysRoom("Standard", 95.00m, 8, "FreeCancellation", StandardAmenities, 4, "Queen bed, breakfast included"),
            new PremierStaysRoom("Deluxe", 150.00m, 3, "FreeCancellation", DeluxeAmenities, 4, "King bed, city view, breakfast included"),
        }),
        new PremierStaysHotel("PS-PAR-001", "Premier Rive Gauche", "Paris", new[]
        {
            new PremierStaysRoom("Deluxe", 210.00m, 5, "FreeCancellation", DeluxeAmenities, 5, "King bed, Eiffel view, breakfast included"),
            new PremierStaysRoom("Suite", 360.00m, 2, "NonRefundable", SuiteAmenities, 5, "Two-room suite, balcony, lounge access"),
        }),
        new PremierStaysHotel("PS-NYC-001", "Premier Midtown", "New York", new[]
        {
            new PremierStaysRoom("Standard", 180.00m, 10, "FreeCancellation", StandardAmenities, 4, "Queen bed, skyline view"),
            new PremierStaysRoom("Deluxe", 260.00m, 4, "NonRefundable", DeluxeAmenities, 5, "King bed, Central Park view, minibar"),
        }),
        new PremierStaysHotel("PS-TOK-001", "Premier Shinjuku", "Tokyo", new[]
        {
            new PremierStaysRoom("Standard", 130.00m, 12, "FreeCancellation", StandardAmenities, 4, "Twin beds, city view"),
            new PremierStaysRoom("Suite", 300.00m, 2, "NonRefundable", SuiteAmenities, 5, "Corner suite, skyline view, onsen access"),
        }),
    };

    public Task<IReadOnlyList<HotelOffer>> SearchAsync(
        SearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var offers = Source
            .Where(hotel => string.Equals(hotel.City, criteria.Destination, StringComparison.OrdinalIgnoreCase))
            .SelectMany(hotel => hotel.Rooms.Select(room => Normalize(hotel, room)))
            .Where(offer => criteria.RoomType is null || offer.RoomType == criteria.RoomType)
            .ToList();

        return Task.FromResult<IReadOnlyList<HotelOffer>>(offers);
    }

    private HotelOffer Normalize(PremierStaysHotel hotel, PremierStaysRoom room) =>
        new(
            ProviderId: ProviderId,
            HotelId: hotel.HotelCode,
            HotelName: hotel.HotelName,
            City: hotel.City,
            RoomType: ParseRoomType(room.RoomType),
            PricePerNight: room.NightlyRate,
            Currency: "GBP",
            AvailableRooms: room.RoomsLeft,
            Description: room.Description,
            CancellationPolicy: MapCancellationPolicy(room.CancellationPolicy),
            Amenities: room.Amenities,
            StarRating: room.StarRating);

    private static RoomType ParseRoomType(string value) => value switch
    {
        "Standard" => RoomType.Standard,
        "Deluxe" => RoomType.Deluxe,
        "Suite" => RoomType.Suite,
        _ => throw new InvalidOperationException($"Unknown room type '{value}'.")
    };

    private static CancellationPolicy MapCancellationPolicy(string value) => value switch
    {
        "FreeCancellation" => CancellationPolicy.FreeCancellation48Hours,
        "NonRefundable" => CancellationPolicy.NonRefundable,
        _ => throw new InvalidOperationException($"Unknown cancellation policy '{value}'.")
    };
}
