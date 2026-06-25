using HotelStay.Api.Domain;
using HotelStay.Api.Storage;

namespace HotelStay.Api.Services;

/// <summary>
/// Outcome category for a reservation attempt. Maps to HTTP status codes at the
/// endpoint: Validation -> 400, DocumentMismatch -> 422, Success -> 201.
/// </summary>
public enum ReservationErrorKind
{
    None,
    Validation,
    DocumentMismatch
}

public sealed record ReservationResult(
    bool Succeeded,
    ReservationErrorKind ErrorKind,
    string? ErrorCode,
    string? ErrorMessage,
    Reservation? Reservation)
{
    public static ReservationResult Success(Reservation reservation) =>
        new(true, ReservationErrorKind.None, null, null, reservation);

    public static ReservationResult ValidationFailure(string code, string message) =>
        new(false, ReservationErrorKind.Validation, code, message, null);

    public static ReservationResult DocumentMismatch(string message) =>
        new(false, ReservationErrorKind.DocumentMismatch, "DocumentMismatch", message, null);
}

public sealed class ReservationService
{
    private readonly DocumentValidator _documentValidator;
    private readonly IReservationStore _store;
    private readonly TimeProvider _timeProvider;

    public ReservationService(
        DocumentValidator documentValidator,
        IReservationStore store,
        TimeProvider timeProvider)
    {
        _documentValidator = documentValidator;
        _store = store;
        _timeProvider = timeProvider;
    }

    public ReservationResult Reserve(ReserveRequest request)
    {
        // 1. Structural validation (400) — evaluated before document rules (422).
        var structural = ValidateStructure(request);
        if (structural is not null)
        {
            return structural;
        }

        // Required fields are guaranteed non-null past this point.
        var city = request.City!;
        var guest = request.Guest!;

        // 2. Document rules. Unknown destination -> 400 (scope undeterminable);
        //    document/scope mismatch -> 422.
        var documentResult = _documentValidator.Validate(city, guest.DocumentType!.Value);
        if (!documentResult.IsValid)
        {
            return documentResult.Error == DocumentValidationError.UnknownDestination
                ? ReservationResult.ValidationFailure("UnknownDestination", documentResult.Message!)
                : ReservationResult.DocumentMismatch(documentResult.Message!);
        }

        // 3. Build the reservation with server-derived nights and total.
        var checkIn = request.CheckIn!.Value;
        var checkOut = request.CheckOut!.Value;
        var nights = checkOut.DayNumber - checkIn.DayNumber;
        var pricePerNight = request.PricePerNight!.Value;

        var reservation = new Reservation(
            Reference: GenerateReference(),
            ProviderId: request.ProviderId!,
            HotelId: request.HotelId!,
            HotelName: request.HotelName!,
            City: city,
            RoomType: request.RoomType!.Value,
            PricePerNight: pricePerNight,
            Currency: string.IsNullOrWhiteSpace(request.Currency) ? "GBP" : request.Currency,
            CheckIn: checkIn,
            CheckOut: checkOut,
            Nights: nights,
            TotalPrice: nights * pricePerNight,
            CancellationPolicy: request.CancellationPolicy,
            Guest: new Guest(guest.FullName!, guest.DocumentType.Value, guest.DocumentNumber!),
            CreatedAt: _timeProvider.GetUtcNow());

        _store.Save(reservation);

        return ReservationResult.Success(reservation);
    }

    // Returns a 400-mapped failure for the first structural problem, or null if valid.
    private static ReservationResult? ValidateStructure(ReserveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderId))
            return ReservationResult.ValidationFailure("MissingField", "providerId is required.");

        if (string.IsNullOrWhiteSpace(request.HotelId))
            return ReservationResult.ValidationFailure("MissingField", "hotelId is required.");

        if (string.IsNullOrWhiteSpace(request.HotelName))
            return ReservationResult.ValidationFailure("MissingField", "hotelName is required.");

        if (string.IsNullOrWhiteSpace(request.City))
            return ReservationResult.ValidationFailure("MissingField", "city is required.");

        if (request.RoomType is null)
            return ReservationResult.ValidationFailure("MissingField", "roomType is required.");

        if (request.PricePerNight is null)
            return ReservationResult.ValidationFailure("MissingField", "pricePerNight is required.");

        if (request.PricePerNight <= 0)
            return ReservationResult.ValidationFailure("InvalidPrice", "pricePerNight must be greater than zero.");

        if (request.CheckIn is null || request.CheckOut is null)
            return ReservationResult.ValidationFailure("MissingField", "checkIn and checkOut are required.");

        if (request.CheckOut.Value.DayNumber <= request.CheckIn.Value.DayNumber)
            return ReservationResult.ValidationFailure("InvalidDateRange", "checkOut must be after checkIn.");

        if (request.Guest is null)
            return ReservationResult.ValidationFailure("MissingField", "guest is required.");

        if (string.IsNullOrWhiteSpace(request.Guest.FullName))
            return ReservationResult.ValidationFailure("MissingField", "guest.fullName is required.");

        if (request.Guest.DocumentType is null)
            return ReservationResult.ValidationFailure("MissingField", "guest.documentType is required.");

        if (string.IsNullOrWhiteSpace(request.Guest.DocumentNumber))
            return ReservationResult.ValidationFailure("MissingField", "guest.documentNumber is required.");

        return null;
    }

    private static string GenerateReference() =>
        $"HS-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
