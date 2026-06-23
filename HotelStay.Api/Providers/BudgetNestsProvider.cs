using HotelStay.Api.Domain;

namespace HotelStay.Api.Providers;

/// <summary>
/// Deterministic stub provider returning minimal-detail rooms. Source data
/// mimics a snake_case upstream feed; unavailable rooms are filtered out.
/// </summary>
public sealed class BudgetNestsProvider : IHotelProvider
{
    public string Name => "BudgetNests";

    // snake_case-style source record, as a raw upstream feed would expose it.
    private sealed record BudgetNestsSourceRoom(
        string room_id,
        string room_type,
        decimal price_per_night,
        string policy,
        bool available);

    // Fixed, deterministic inventory — note the unavailable Suite row.
    private static readonly IReadOnlyList<BudgetNestsSourceRoom> Source = new[]
    {
        new BudgetNestsSourceRoom("BN-STD-001", "Standard", 65.00m, "Flexible", true),
        new BudgetNestsSourceRoom("BN-DLX-001", "Deluxe", 95.00m, "NonRefundable", true),
        new BudgetNestsSourceRoom("BN-STE-001", "Suite", 150.00m, "Flexible", false),
    };

    public Task<IReadOnlyList<HotelRoomOption>> SearchAsync(
        HotelSearchRequest request,
        CancellationToken cancellationToken)
    {
        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;

        var results = Source
            .Where(source => source.available)
            .Select(MapToOption(request.Destination, nights))
            .Where(option => request.RoomType is null || option.RoomType == request.RoomType)
            .ToList();

        return Task.FromResult<IReadOnlyList<HotelRoomOption>>(results);
    }

    private Func<BudgetNestsSourceRoom, HotelRoomOption> MapToOption(string destination, int nights) =>
        source => new HotelRoomOption(
            Id: source.room_id,
            Provider: Name,
            Destination: destination,
            RoomType: ParseRoomType(source.room_type),
            PerNightRate: source.price_per_night,
            TotalPrice: source.price_per_night * nights,
            Nights: nights,
            CancellationPolicy: MapCancellationPolicy(source.policy),
            Amenities: Array.Empty<string>(),
            StarRating: 3);

    private static RoomType ParseRoomType(string value) => value switch
    {
        "Standard" => RoomType.Standard,
        "Deluxe" => RoomType.Deluxe,
        "Suite" => RoomType.Suite,
        _ => throw new InvalidOperationException($"Unknown room type '{value}'.")
    };

    private static CancellationPolicy MapCancellationPolicy(string value) => value switch
    {
        "Flexible" => CancellationPolicy.Flexible24Hours,
        "NonRefundable" => CancellationPolicy.NonRefundable,
        _ => throw new InvalidOperationException($"Unknown cancellation policy '{value}'.")
    };
}
