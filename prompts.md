# AI Prompts and Decisions

This file documents the significant AI prompts used during the Hotel Stay Availability challenge and the key judgement calls made during implementation.

AI tool used:
- Claude in VS Code

## Prompt - Specification and initial design

Prompt:
"Create a spec.md for the Hotel Stay Availability challenge. Include assumptions, domain models, provider interface contract, API contracts, validation rules, testing strategy, and extensibility notes. The backend is .NET 8+ Minimal API, the frontend is Angular, and tests are xUnit. The system must query two deterministic stub providers, normalize results, validate documents during reservation, and support adding a third provider later."

Output used:
Claude helped draft the initial specification and design outline.

Decision:
Defined the home country as the UK. Domestic cities are London and Manchester. International cities are Paris, New York, and Tokyo. Domestic destinations accept National ID or Passport, while international destinations require Passport.

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

## Prompt - Angular frontend design

Prompt:
"I am building the Hotel Stay Availability challenge. Backend is already implemented as a .NET Minimal API with GET /hotels/search, POST /hotels/reserve, and GET /hotels/reservation/{reference}. Frontend must be Angular. Requirements: search form with destination, check-in, check-out, optional room type; results list with provider badge, room type, per-night rate, total price, and cancellation policy; sortable by total price; reservation form with guest name, document type, and document number; client-side document validation; confirmation display with reference number, provider, total price, and cancellation policy. Propose the Angular component structure, models, and service design before generating code."

Output used:
Claude proposed the Angular frontend structure, including models, API service, and UI state handling.

Decision:
Kept the frontend simple and demo-focused. Used a small Angular structure with typed models, a hotel API service, and UI states for loading, results, empty results, errors, reservation, and confirmation.

## Prompt - Angular models and API service

Prompt:
"Generate Angular models and an API service for the Hotel Stay Availability frontend. Create TypeScript interfaces for HotelRoomOption, ReserveRoomRequest, and ReservationResponse. Create HotelApiService with methods for searchHotels, reserveRoom, and getReservation. Use Angular HttpClient and match the backend JSON shape."

Output used:
Claude generated the TypeScript interfaces and Angular service for communicating with the backend.

Decision:
Kept frontend model names aligned with backend DTOs to reduce unnecessary mapping and make the API contract easier to understand.

## Prompt - Angular UI implementation

Prompt:
"Generate a simple Angular UI for the Hotel Stay Availability app. Use the existing HotelApiService and models. Implement search form, loading state, error state, empty results state, results display, sorting by total price, reservation form, client-side document validation, and confirmation panel. Use destination options London, Manchester, Paris, New York, and Tokyo. International destinations should require Passport."

Output used:
Claude generated the initial Angular UI implementation.

Decision:
Prioritized a clear interview demo over complex UI architecture. The UI shows all required states and supports the full end-to-end search and reservation flow.

## Prompt - Frontend validation review

Prompt:
"Review the Angular reservation flow and confirm that client-side document validation matches the backend validation rules. International destinations should require Passport, while domestic destinations should accept National ID or Passport."

Output used:
Claude reviewed the validation logic and helped confirm alignment between frontend and backend behavior.

Decision:
Kept document validation in both frontend and backend. The frontend improves user experience, while the backend remains the source of truth and returns 422 for invalid reservations.

## Prompt - Documentation

Prompt:
"Generate README.md and reflection.md for this Hotel Stay Availability challenge. README.md should include project overview, prerequisites, backend run steps, frontend run steps, test command, Swagger URL guidance, supported destinations, assumptions, API endpoints, and clean clone instructions. reflection.md should include what went well, key design decisions, how AI was used, and what I would improve with more time."

Output used:
Claude generated the first draft of README.md and reflection.md.

Decision:
Kept documentation focused on clean clone setup, local demo readiness, assumptions, and honest AI usage.

## Prompt - Final review

Prompt:
"Review the completed Hotel Stay Availability solution against the challenge requirements. Check the backend API endpoints, provider abstraction, deterministic stubs, document validation, Angular frontend states, tests, README, prompts.md, and reflection.md. Identify any missing items before submission."

Output used:
Claude helped perform a final checklist review before submission.

Decision:
Confirmed that the solution runs locally, supports the required API endpoints, includes meaningful tests, documents AI usage, and provides an Angular UI for the end-to-end reservation flow.