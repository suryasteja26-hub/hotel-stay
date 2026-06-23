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
    string? ErrorMessage,
    ReservationResponse? Reservation)
{
    public static ReservationResult Success(ReservationResponse reservation) =>
        new(true, ReservationErrorKind.None, null, reservation);

    public static ReservationResult ValidationFailure(string message) =>
        new(false, ReservationErrorKind.Validation, message, null);

    public static ReservationResult DocumentMismatch(string message) =>
        new(false, ReservationErrorKind.DocumentMismatch, message, null);
}

public sealed class ReservationService
{
    private readonly DocumentValidator _documentValidator;
    private readonly IReservationStore _store;

    public ReservationService(DocumentValidator documentValidator, IReservationStore store)
    {
        _documentValidator = documentValidator;
        _store = store;
    }

    public ReservationResult Reserve(ReserveRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GuestName))
        {
            return ReservationResult.ValidationFailure("Guest name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DocumentNumber))
        {
            return ReservationResult.ValidationFailure("Document number is required.");
        }

        if (request.CheckOut.DayNumber <= request.CheckIn.DayNumber)
        {
            return ReservationResult.ValidationFailure("Check-out must be after check-in.");
        }

        var documentResult = _documentValidator.Validate(request.Destination, request.DocumentType);
        if (!documentResult.IsValid)
        {
            return documentResult.Error == DocumentValidationError.UnknownDestination
                ? ReservationResult.ValidationFailure(documentResult.Message!)
                : ReservationResult.DocumentMismatch(documentResult.Message!);
        }

        var reservation = new ReservationResponse(
            Reference: GenerateReference(),
            GuestName: request.GuestName,
            Provider: request.Provider,
            Destination: request.Destination,
            CheckIn: request.CheckIn,
            CheckOut: request.CheckOut,
            RoomType: request.RoomType,
            TotalPrice: request.TotalPrice,
            CancellationPolicy: request.CancellationPolicy,
            DocumentType: request.DocumentType,
            DocumentNumber: request.DocumentNumber);

        _store.Save(reservation);

        return ReservationResult.Success(reservation);
    }

    private static string GenerateReference() =>
        $"HS-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
