using HotelStay.Api.Domain;
using HotelStay.Api.Services;
using HotelStay.Api.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelStay.Tests;

public class ReservationServiceTests
{
    private static ReservationService CreateService(out IReservationStore store)
    {
        store = new InMemoryReservationStore();
        return new ReservationService(
            new DocumentValidator(new DestinationRules()),
            store,
            TimeProvider.System,
            NullLogger<ReservationService>.Instance);
    }

    private static ReserveRequest ValidRequest(
        string city = "London",
        DocumentType documentType = DocumentType.NationalId,
        decimal pricePerNight = 120.00m) =>
        new(
            ProviderId: "PremierStays",
            HotelId: "PS-LON-001",
            HotelName: "Premier Thames View",
            City: city,
            RoomType: RoomType.Deluxe,
            PricePerNight: pricePerNight,
            Currency: "GBP",
            CheckIn: new DateOnly(2026, 7, 10),
            CheckOut: new DateOnly(2026, 7, 13),
            Guest: new GuestRequest("Jane Doe", documentType, "X1234567"),
            CancellationPolicy: CancellationPolicy.FreeCancellation48Hours);

    [Fact]
    public void Valid_domestic_reservation_succeeds_and_derives_nights_and_total()
    {
        var service = CreateService(out _);

        var result = service.Reserve(ValidRequest(pricePerNight: 100m));

        Assert.True(result.Succeeded);
        var reservation = result.Reservation!;
        Assert.Equal(3, reservation.Nights);
        Assert.Equal(300m, reservation.TotalPrice);        // 3 * 100
        Assert.StartsWith("HS-", reservation.Reference);
    }

    [Fact]
    public void International_destination_with_NationalId_is_document_mismatch_422()
    {
        var service = CreateService(out _);

        var result = service.Reserve(ValidRequest("Paris", DocumentType.NationalId));

        Assert.False(result.Succeeded);
        Assert.Equal(ReservationErrorKind.DocumentMismatch, result.ErrorKind);
    }

    [Fact]
    public void Unknown_destination_is_validation_failure_400()
    {
        var service = CreateService(out _);

        var result = service.Reserve(ValidRequest("Atlantis", DocumentType.Passport));

        Assert.False(result.Succeeded);
        Assert.Equal(ReservationErrorKind.Validation, result.ErrorKind);
    }

    [Theory]
    [InlineData(null, "PS-1", "Hotel")]   // missing providerId
    [InlineData("PS", null, "Hotel")]     // missing hotelId
    [InlineData("PS", "PS-1", null)]      // missing hotelName
    public void Missing_required_fields_fail_structural_validation(string? providerId, string? hotelId, string? hotelName)
    {
        var service = CreateService(out _);
        var request = ValidRequest() with { ProviderId = providerId, HotelId = hotelId, HotelName = hotelName };

        var result = service.Reserve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(ReservationErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public void CheckOut_not_after_checkIn_fails_validation()
    {
        var service = CreateService(out _);
        var request = ValidRequest() with { CheckOut = new DateOnly(2026, 7, 10) };

        var result = service.Reserve(request);

        Assert.False(result.Succeeded);
        Assert.Equal(ReservationErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public void Saved_reservation_round_trips_through_store()
    {
        var service = CreateService(out var store);

        var result = service.Reserve(ValidRequest());

        var fetched = store.Get(result.Reservation!.Reference);
        Assert.NotNull(fetched);
        Assert.Equal(result.Reservation.Reference, fetched!.Reference);
    }

    [Fact]
    public void References_are_unique_across_many_reservations()
    {
        var service = CreateService(out _);

        var references = Enumerable.Range(0, 200)
            .Select(_ => service.Reserve(ValidRequest()).Reservation!.Reference)
            .ToList();

        Assert.Equal(references.Count, references.Distinct().Count());
    }
}
