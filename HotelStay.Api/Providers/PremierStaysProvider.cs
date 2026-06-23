using HotelStay.Api.Domain;

namespace HotelStay.Api.Providers;

/// <summary>
/// Deterministic stub provider returning full-detail, always-available rooms.
/// Source data mimics a PascalCase upstream feed and is normalized on read.
/// </summary>
public sealed class PremierStaysProvider : IHotelProvider
{
    public string Name => "PremierStays";

    // PascalCase-style source record, as a raw upstream feed would expose it.
    private sealed record PremierStaysSourceRoom(
        string RoomId,
        string RoomType,
        decimal NightlyRate,
        string CancellationPolicy,
        string[] Amenities,
        int StarRating);

    // Fixed, deterministic inventory — every room is available.
    private static readonly IReadOnlyList<PremierStaysSourceRoom> Source = new[]
    {
        new PremierStaysSourceRoom(
            "PS-STD-001", "Standard", 120.00m, "FreeCancellation",
            new[] { "WiFi", "Air Conditioning", "Breakfast" }, 4),
        new PremierStaysSourceRoom(
            "PS-DLX-001", "Deluxe", 185.00m, "FreeCancellation",
            new[] { "WiFi", "Air Conditioning", "Breakfast", "City View", "Minibar" }, 4),
        new PremierStaysSourceRoom(
            "PS-STE-001", "Suite", 320.00m, "NonRefundable",
            new[] { "WiFi", "Air Conditioning", "Breakfast", "Lounge Access", "Minibar", "Spa" }, 5),
    };

    public Task<IReadOnlyList<HotelRoomOption>> SearchAsync(
        HotelSearchRequest request,
        CancellationToken cancellationToken)
    {
        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;

        var results = Source
            .Select(MapToOption(request.Destination, nights))
            .Where(option => request.RoomType is null || option.RoomType == request.RoomType)
            .ToList();

        return Task.FromResult<IReadOnlyList<HotelRoomOption>>(results);
    }

    private Func<PremierStaysSourceRoom, HotelRoomOption> MapToOption(string destination, int nights) =>
        source => new HotelRoomOption(
            Id: source.RoomId,
            Provider: Name,
            Destination: destination,
            RoomType: ParseRoomType(source.RoomType),
            PerNightRate: source.NightlyRate,
            TotalPrice: source.NightlyRate * nights,
            Nights: nights,
            CancellationPolicy: MapCancellationPolicy(source.CancellationPolicy),
            Amenities: source.Amenities,
            StarRating: source.StarRating);

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
