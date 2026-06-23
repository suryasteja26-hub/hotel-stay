# AI Prompts and Decisions

## Prompt - Domain model generation

Prompt:
"Generate the domain models for the HotelStay.Api .NET Minimal API. Include enums for RoomType, DocumentType, and CancellationPolicy. Include records for HotelSearchRequest, HotelRoomOption, ReserveRoomRequest, and ReservationResponse. Use DateOnly for dates, decimal for prices, and IReadOnlyList<string> for amenities."

Output used:
Claude generated the initial domain enums and records.

Decision:
Used DateOnly for dates, decimal for prices, and a unified enum model to normalize provider-specific data.

## Prompt - Provider abstraction and stubs

Prompt:
"Generate IHotelProvider and two deterministic stub provider implementations: PremierStaysProvider and BudgetNestsProvider. PremierStays should use PascalCase-style source data, always return available rooms, and include amenities and star rating. BudgetNests should use snake_case-style source data, include an available flag, filter unavailable rooms, and return minimal details. Both providers should normalize to HotelRoomOption and support Standard, Deluxe, and Suite."

Output used:
Claude generated the provider interface and initial provider implementations.

Decision:
Kept provider-specific source records inside each provider and returned only normalized HotelRoomOption records from the provider boundary. BudgetNests unavailable rooms are filtered inside the provider.

## Prompt - Search, validation, and reservation services

Prompt:
"Generate services for hotel search, destination rules, document validation, reservation creation, and in-memory reservation storage. HotelSearchService should query all IHotelProvider implementations and sort by total price. DocumentValidator should enforce that international destinations require Passport and domestic destinations accept National ID or Passport."

Output used:
Claude generated the service classes and in-memory store abstraction.

Decision:
Kept document validation in a dedicated service so it can be unit tested independently and reused by the reservation endpoint.

## Prompt - Minimal API endpoints

Prompt:
"Update Program.cs for a .NET Minimal API. Register providers and services using dependency injection. Add GET /hotels/search, POST /hotels/reserve, and GET /hotels/reservation/{reference}. Return 400 for missing or invalid search parameters, 422 for document mismatch, and 404 for unknown reservation reference. Enable Swagger and CORS for Angular."

Output used:
Claude generated the initial Minimal API endpoint implementation.

Decision:
Used explicit Minimal API validation in the endpoint layer for request-shape errors and delegated business validation to services.

## Prompt - Backend tests

Prompt:
"Generate meaningful xUnit tests for the HotelStay.Api project. Cover document validation, provider normalization, unavailable room filtering, total price calculation, room type filtering, search result sorting, and API error cases."

Output used:
Claude generated the first set of unit and integration test ideas.

Decision:
Focused the test suite on challenge-specific business rules rather than simple object construction.