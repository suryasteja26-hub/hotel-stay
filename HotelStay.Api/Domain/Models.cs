using System.Text.Json.Serialization;

namespace HotelStay.Api.Domain;

public enum RoomType
{
    Standard,
    Deluxe,
    Suite
}

public enum DocumentType
{
    NationalId,
    Passport
}

public enum CancellationPolicy
{
    FreeCancellation48Hours,
    Flexible24Hours,
    NonRefundable
}

/// <summary>
/// Normalized search criteria the aggregator passes to every provider.
/// <c>RoomType == null</c> means "all room types".
/// </summary>
public record SearchCriteria(
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    RoomType? RoomType);

/// <summary>
/// A single normalized, bookable offer. The only shape the aggregator and API
/// surface — providers never leak their raw models above this.
/// </summary>
public record HotelOffer(
    string ProviderId,
    string HotelId,
    string HotelName,
    string City,
    RoomType RoomType,
    decimal PricePerNight,
    string Currency,
    int? AvailableRooms,
    string? Description,
    CancellationPolicy CancellationPolicy,
    IReadOnlyList<string> Amenities,
    int? StarRating);

/// <summary>
/// Search response wrapper: echoes the query and carries the derived nights
/// count alongside the normalized offer list.
/// </summary>
public record SearchResponse(
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    IReadOnlyList<HotelOffer> Results);

/// <summary>
/// Reserve request body. Fields are nullable so missing values can be detected
/// and reported as structural (400) errors rather than silently defaulted.
/// </summary>
public record ReserveRequest(
    string? ProviderId,
    string? HotelId,
    string? HotelName,
    string? City,
    RoomType? RoomType,
    decimal? PricePerNight,
    string? Currency,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    GuestRequest? Guest,
    CancellationPolicy? CancellationPolicy);

public record GuestRequest(
    string? FullName,
    DocumentType? DocumentType,
    string? DocumentNumber);

/// <summary>
/// Confirmed reservation — also the GET-by-reference shape. Nights and
/// TotalPrice are server-derived; the document number is never serialized back.
/// </summary>
public record Reservation(
    string Reference,
    string ProviderId,
    string HotelId,
    string HotelName,
    string City,
    RoomType RoomType,
    decimal PricePerNight,
    string Currency,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    decimal TotalPrice,
    CancellationPolicy? CancellationPolicy,
    Guest Guest,
    DateTimeOffset CreatedAt);

public record Guest(
    string FullName,
    DocumentType DocumentType,
    [property: JsonIgnore] string DocumentNumber);

/// <summary>Consistent ProblemDetails-compatible error envelope for all non-2xx responses.</summary>
public record ApiError(int Status, string Error, string Message);
