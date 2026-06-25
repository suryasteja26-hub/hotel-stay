using System.Text.Json.Serialization;
using HotelStay.Api.Domain;
using HotelStay.Api.Providers;
using HotelStay.Api.Services;
using HotelStay.Api.Storage;
using Microsoft.AspNetCore.Diagnostics;

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
builder.Services.AddSingleton<IReservationStore, FileReservationStore>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(AngularCorsPolicy);

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var error = exceptionHandlerFeature?.Error;

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var apiError = new ApiError(
            Status: 500,
            Error: "UnhandledException",
            Message: error?.Message ?? "An unexpected error occurred.");

        await context.Response.WriteAsJsonAsync(apiError);
    });
});

app.MapGet("/hotels/destinations", (DestinationRules rules) =>
{
    var domestic = rules.GetDomestic();
    var international = rules.GetInternational();
    return Results.Ok(new { domestic, international });
})
.WithName("GetDestinations");

if (app.Environment.IsDevelopment())
{
    app.MapGet("/test/throw", (HttpContext _) => throw new InvalidOperationException("Test exception"));
}

// Consistent ProblemDetails-compatible error envelope: { status, error, message }.
static IResult Problem(int status, string error, string message) =>
    Results.Json(new ApiError(status, error, message), statusCode: status);

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
        return Problem(400, "MissingParameter", "destination, checkIn and checkOut are required.");
    }

    if (!DateOnly.TryParse(checkIn, out var checkInDate))
    {
        return Problem(400, "InvalidDate", "checkIn must be a valid date (yyyy-MM-dd).");
    }

    if (!DateOnly.TryParse(checkOut, out var checkOutDate))
    {
        return Problem(400, "InvalidDate", "checkOut must be a valid date (yyyy-MM-dd).");
    }

    if (checkOutDate.DayNumber <= checkInDate.DayNumber)
    {
        return Problem(400, "InvalidDateRange", "checkOut must be after checkIn.");
    }

    RoomType? roomTypeFilter = null;
    if (!string.IsNullOrWhiteSpace(roomType))
    {
        if (!Enum.TryParse<RoomType>(roomType, ignoreCase: true, out var parsedRoomType))
        {
            return Problem(400, "InvalidRoomType", $"Unknown room type '{roomType}'.");
        }

        roomTypeFilter = parsedRoomType;
    }

    var criteria = new SearchCriteria(destination, checkInDate, checkOutDate, roomTypeFilter);
    var response = await searchService.SearchAsync(criteria, cancellationToken);

    return Results.Ok(response);
})
.WithName("SearchHotels");

app.MapPost("/hotels/reserve", (
    ReserveRequest request,
    ReservationService reservationService) =>
{
    var result = reservationService.Reserve(request);

    if (result.Succeeded)
    {
        return Results.Created(
            $"/hotels/reservation/{result.Reservation!.Reference}",
            result.Reservation);
    }

    return result.ErrorKind == ReservationErrorKind.DocumentMismatch
        ? Problem(422, result.ErrorCode!, result.ErrorMessage!)
        : Problem(400, result.ErrorCode!, result.ErrorMessage!);
})
.WithName("ReserveRoom");

app.MapGet("/hotels/reservation/{reference}", (
    string reference,
    IReservationStore store) =>
{
    var reservation = store.Get(reference);

    return reservation is not null
        ? Results.Ok(reservation)
        : Problem(404, "NotFound", $"Reservation '{reference}' was not found.");
})
.WithName("GetReservation");

app.Run();

// Exposed so the xUnit integration tests can drive the app via WebApplicationFactory.
public partial class Program { }
