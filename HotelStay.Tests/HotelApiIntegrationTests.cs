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

    // ---- Search ----

    [Fact]
    public async Task Search_returns_wrapper_with_metadata_and_merged_results()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/hotels/search?destination=London&checkIn=2026-07-10&checkOut=2026-07-13");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SearchResponse>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal("London", body!.Destination);
        Assert.Equal(new DateOnly(2026, 7, 10), body.CheckIn);
        Assert.Equal(new DateOnly(2026, 7, 13), body.CheckOut);
        Assert.Equal(3, body.Nights);
        Assert.NotEmpty(body.Results);
        // Both providers serve London.
        Assert.Contains(body.Results, o => o.ProviderId == "PremierStays");
        Assert.Contains(body.Results, o => o.ProviderId == "BudgetNests");
    }

    [Fact]
    public async Task Search_results_are_sorted_by_pricePerNight_ascending()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/hotels/search?destination=London&checkIn=2026-07-10&checkOut=2026-07-13");

        var body = await response.Content.ReadFromJsonAsync<SearchResponse>(JsonOptions);
        var prices = body!.Results.Select(o => o.PricePerNight).ToList();

        Assert.Equal(prices.OrderBy(p => p), prices);
    }

    [Fact]
    public async Task Search_unknown_city_returns_200_with_empty_results()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/hotels/search?destination=Atlantis&checkIn=2026-07-10&checkOut=2026-07-13");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SearchResponse>(JsonOptions);
        Assert.Empty(body!.Results);
    }

    [Fact]
    public async Task Search_roomType_filter_narrows_results()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/hotels/search?destination=London&checkIn=2026-07-10&checkOut=2026-07-13&roomType=Suite");

        var body = await response.Content.ReadFromJsonAsync<SearchResponse>(JsonOptions);
        Assert.All(body!.Results, o => Assert.Equal(RoomType.Suite, o.RoomType));
    }

    [Theory]
    [InlineData("/hotels/search?checkIn=2026-07-10&checkOut=2026-07-13")]   // missing destination
    [InlineData("/hotels/search?destination=London&checkOut=2026-07-13")]   // missing checkIn
    [InlineData("/hotels/search?destination=London&checkIn=2026-07-10")]    // missing checkOut
    public async Task Search_returns_400_when_required_fields_missing(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorEnvelope(response, 400);
    }

    [Theory]
    [InlineData("/hotels/search?destination=London&checkIn=2026-07-13&checkOut=2026-07-13")] // equal
    [InlineData("/hotels/search?destination=London&checkIn=2026-07-13&checkOut=2026-07-10")] // before
    [InlineData("/hotels/search?destination=London&checkIn=not-a-date&checkOut=2026-07-13")] // malformed
    public async Task Search_returns_400_for_bad_dates(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Reserve ----

    [Fact]
    public async Task Reserve_returns_201_with_reference_for_valid_international_passport()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/hotels/reserve", BuildReserveRequest("Paris", "Passport"), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reservation = await response.Content.ReadFromJsonAsync<Reservation>(JsonOptions);
        Assert.NotNull(reservation);
        Assert.False(string.IsNullOrWhiteSpace(reservation!.Reference));
        Assert.Equal("Paris", reservation.City);
        Assert.Equal(3, reservation.Nights);
    }

    [Fact]
    public async Task Reserve_returns_201_for_valid_domestic_nationalId()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/hotels/reserve", BuildReserveRequest("London", "NationalId"), JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Reserve_returns_422_when_NationalId_used_for_international_destination()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/hotels/reserve", BuildReserveRequest("Paris", "NationalId"), JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        await AssertErrorEnvelope(response, 422);
    }

    [Fact]
    public async Task Reserve_returns_400_when_required_field_missing()
    {
        var client = _factory.CreateClient();
        var request = BuildReserveRequest("London", "NationalId");
        request.Remove("hotelName");

        var response = await client.PostAsJsonAsync("/hotels/reserve", request, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Reservation lookup ----

    [Fact]
    public async Task Get_reservation_returns_saved_reservation_then_404_for_unknown()
    {
        var client = _factory.CreateClient();

        var reserveResponse = await client.PostAsJsonAsync("/hotels/reserve", BuildReserveRequest("London", "NationalId"), JsonOptions);
        var saved = await reserveResponse.Content.ReadFromJsonAsync<Reservation>(JsonOptions);

        var getResponse = await client.GetAsync($"/hotels/reservation/{saved!.Reference}");
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<Reservation>(JsonOptions);
        Assert.Equal(saved.Reference, fetched!.Reference);

        var missing = await client.GetAsync("/hotels/reservation/HS-DOESNOTEXIST");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // ---- Helpers ----

    private static async Task AssertErrorEnvelope(HttpResponseMessage response, int expectedStatus)
    {
        var error = await response.Content.ReadFromJsonAsync<ApiError>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal(expectedStatus, error!.Status);
        Assert.False(string.IsNullOrWhiteSpace(error.Error));
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    // Built as a dictionary so individual fields can be removed to test 400s.
    private static Dictionary<string, object> BuildReserveRequest(string city, string documentType) =>
        new()
        {
            ["providerId"] = "PremierStays",
            ["hotelId"] = "PS-DLX-001",
            ["hotelName"] = "Premier Thames View",
            ["city"] = city,
            ["roomType"] = "Deluxe",
            ["pricePerNight"] = 185.00m,
            ["currency"] = "GBP",
            ["checkIn"] = "2026-07-10",
            ["checkOut"] = "2026-07-13",
            ["cancellationPolicy"] = "FreeCancellation48Hours",
            ["guest"] = new Dictionary<string, object>
            {
                ["fullName"] = "Jane Doe",
                ["documentType"] = documentType,
                ["documentNumber"] = "X1234567",
            },
        };
}
