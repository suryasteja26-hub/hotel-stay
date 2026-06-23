using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelStay.Api.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HotelStay.Tests;

public class HotelApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public HotelApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/hotels/search?checkIn=2026-07-10&checkOut=2026-07-13")]                  // missing destination
    [InlineData("/hotels/search?destination=London&checkOut=2026-07-13")]                  // missing checkIn
    [InlineData("/hotels/search?destination=London&checkIn=2026-07-10")]                   // missing checkOut
    public async Task Search_returns_400_when_required_fields_missing(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_returns_400_when_checkOut_not_after_checkIn()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/hotels/search?destination=London&checkIn=2026-07-13&checkOut=2026-07-13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reserve_returns_422_when_NationalId_used_for_international_destination()
    {
        var client = _factory.CreateClient();
        var request = BuildReserveRequest("Paris", DocumentType.NationalId);

        var response = await client.PostAsJsonAsync("/hotels/reserve", request, JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Reserve_succeeds_with_reference_when_Passport_used_for_international_destination()
    {
        var client = _factory.CreateClient();
        var request = BuildReserveRequest("Paris", DocumentType.Passport);

        var response = await client.PostAsJsonAsync("/hotels/reserve", request, JsonOptions);

        response.EnsureSuccessStatusCode();
        var reservation = await response.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions);

        Assert.NotNull(reservation);
        Assert.False(string.IsNullOrWhiteSpace(reservation!.Reference));
        Assert.Equal("Paris", reservation.Destination);
    }

    [Fact]
    public async Task Get_reservation_returns_the_saved_reservation()
    {
        var client = _factory.CreateClient();
        var request = BuildReserveRequest("London", DocumentType.NationalId);

        var reserveResponse = await client.PostAsJsonAsync("/hotels/reserve", request, JsonOptions);
        reserveResponse.EnsureSuccessStatusCode();
        var saved = await reserveResponse.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions);

        var getResponse = await client.GetAsync($"/hotels/reservation/{saved!.Reference}");

        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions);

        Assert.NotNull(fetched);
        Assert.Equal(saved.Reference, fetched!.Reference);
        Assert.Equal(saved.GuestName, fetched.GuestName);
        Assert.Equal(saved.Destination, fetched.Destination);
    }

    private static ReserveRoomRequest BuildReserveRequest(string destination, DocumentType documentType) =>
        new(
            RoomId: "PS-DLX-001",
            Provider: "PremierStays",
            Destination: destination,
            CheckIn: new DateOnly(2026, 7, 10),
            CheckOut: new DateOnly(2026, 7, 13),
            RoomType: RoomType.Deluxe,
            TotalPrice: 555.00m,
            GuestName: "Jane Doe",
            DocumentType: documentType,
            DocumentNumber: "X1234567",
            CancellationPolicy: CancellationPolicy.FreeCancellation48Hours);
}
