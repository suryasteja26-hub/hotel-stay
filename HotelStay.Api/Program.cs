using System.Text.Json.Serialization;
using HotelStay.Api.Domain;
using HotelStay.Api.Providers;
using HotelStay.Api.Services;
using HotelStay.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

const string AngularCorsPolicy = "AngularDev";

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddPolicy(AngularCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Providers — registering another IHotelProvider here is the only change
// needed to add a third source.
builder.Services.AddSingleton<IHotelProvider, PremierStaysProvider>();
builder.Services.AddSingleton<IHotelProvider, BudgetNestsProvider>();

builder.Services.AddSingleton<HotelSearchService>();
builder.Services.AddSingleton<DestinationRules>();
builder.Services.AddSingleton<DocumentValidator>();
builder.Services.AddSingleton<ReservationService>();
builder.Services.AddSingleton<IReservationStore, InMemoryReservationStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(AngularCorsPolicy);

app.MapGet("/hotels/search", async (
    string? destination,
    string? checkIn,
    string? checkOut,
    string? roomType,
    HotelSearchService searchService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(destination)
        || string.IsNullOrWhiteSpace(checkIn)
        || string.IsNullOrWhiteSpace(checkOut))
    {
        return Results.BadRequest(new { message = "destination, checkIn and checkOut are required." });
    }

    if (!DateOnly.TryParse(checkIn, out var checkInDate))
    {
        return Results.BadRequest(new { message = "checkIn must be a valid date (yyyy-MM-dd)." });
    }

    if (!DateOnly.TryParse(checkOut, out var checkOutDate))
    {
        return Results.BadRequest(new { message = "checkOut must be a valid date (yyyy-MM-dd)." });
    }

    if (checkOutDate.DayNumber <= checkInDate.DayNumber)
    {
        return Results.BadRequest(new { message = "checkOut must be after checkIn." });
    }

    RoomType? roomTypeFilter = null;
    if (!string.IsNullOrWhiteSpace(roomType))
    {
        if (!Enum.TryParse<RoomType>(roomType, ignoreCase: true, out var parsedRoomType))
        {
            return Results.BadRequest(new { message = $"Unknown room type '{roomType}'." });
        }

        roomTypeFilter = parsedRoomType;
    }

    var request = new HotelSearchRequest(destination, checkInDate, checkOutDate, roomTypeFilter);
    var results = await searchService.SearchAsync(request, cancellationToken);

    return Results.Ok(results);
})
.WithName("SearchHotels");

app.MapPost("/hotels/reserve", (
    ReserveRoomRequest request,
    ReservationService reservationService) =>
{
    var result = reservationService.Reserve(request);

    if (result.Succeeded)
    {
        return Results.Ok(result.Reservation);
    }

    return result.ErrorKind == ReservationErrorKind.DocumentMismatch
        ? Results.UnprocessableEntity(new { message = result.ErrorMessage })
        : Results.BadRequest(new { message = result.ErrorMessage });
})
.WithName("ReserveRoom");

app.MapGet("/hotels/reservation/{reference}", (
    string reference,
    IReservationStore store) =>
{
    var reservation = store.Get(reference);

    return reservation is not null
        ? Results.Ok(reservation)
        : Results.NotFound(new { message = $"Reservation '{reference}' was not found." });
})
.WithName("GetReservation");

app.Run();

// Exposed so the xUnit integration tests can drive the app via WebApplicationFactory.
public partial class Program { }
