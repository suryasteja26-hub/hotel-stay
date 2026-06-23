namespace HotelStay.Api.Domain;

public enum RoomType
{
    Standard,
    Deluxe,
    Suite
}

public enum DocumentType
{
    Passport,
    NationalId
}

public enum CancellationPolicy
{
    FreeCancellation48Hours,
    Flexible24Hours,
    NonRefundable
}

public record HotelSearchRequest(
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    RoomType? RoomType);

public record HotelRoomOption(
    string Id,
    string Provider,
    string Destination,
    RoomType RoomType,
    decimal PerNightRate,
    decimal TotalPrice,
    int Nights,
    CancellationPolicy CancellationPolicy,
    IReadOnlyList<string> Amenities,
    int StarRating);

public record ReserveRoomRequest(
    string RoomId,
    string Provider,
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    RoomType RoomType,
    decimal TotalPrice,
    string GuestName,
    DocumentType DocumentType,
    string DocumentNumber,
    CancellationPolicy CancellationPolicy);

public record ReservationResponse(
    string Reference,
    string GuestName,
    string Provider,
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    RoomType RoomType,
    decimal TotalPrice,
    CancellationPolicy CancellationPolicy,
    DocumentType DocumentType,
    string DocumentNumber);
