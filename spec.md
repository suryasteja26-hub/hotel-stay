# Hotel Stay Availability — Specification

## 1. Overview

The Hotel Stay service aggregates hotel availability from multiple third-party
providers, normalizes their heterogeneous responses into a single unified model,
and exposes search, reservation, and reservation-lookup endpoints.

The system ships with two deterministic stub providers:

- **PremierStays** — full-detail provider, PascalCase-style source data, always available.
- **BudgetNests** — minimal-detail provider, snake_case-style source data, may flag rooms as unavailable (these are filtered out).

The architecture is provider-agnostic: a third provider can be added by
implementing one interface and registering it, with no change to the search,
normalization, or reservation flow.

| Aspect    | Choice                                |
|-----------|---------------------------------------|
| Backend   | .NET 8+ Minimal API (current solution targets net9.0) |
| Frontend  | Angular (separate SPA, consumes the API) |
| Tests     | xUnit                                 |
| Storage   | In-memory only (no database)          |

---

## 2. Assumptions

1. **Stub providers are deterministic.** Given the same search input, each
   provider returns the same fixed result set every time. No external network
   calls, no randomness, no clock dependence.
2. **No authentication / authorization.** Endpoints are open; there is no user
   or tenant concept. Reservations are not tied to an account.
3. **No payment handling.** Reserving returns a reference number; no money moves.
4. **Currency is a single fixed currency** (assume GBP) and prices are stored as
   decimal amounts per night. No FX conversion.
5. **Dates are calendar dates** (`yyyy-MM-dd`), interpreted without time zones.
   `checkIn` and `checkOut` are date-only.
6. **Destination matching is by city name**, case-insensitive, against the known
   domestic/international city list. Unknown cities are treated as having no
   inventory (empty result set), not an error.
7. **Room types are a closed enum:** `Standard`, `Deluxe`, `Suite`. Both providers
   support all three.
8. **Availability filtering is provider-side semantics:** BudgetNests rows with
   `available = false` are dropped during normalization and never surface in results.
9. **Reservation references are server-generated, opaque, and unique** for the
   process lifetime. They are not guessable-by-design but need not be
   cryptographically secure for this challenge.
10. **In-memory persistence is per-process.** All reservations are lost on restart.
    Concurrency is handled with a thread-safe store but no durability guarantees.
11. **A reservation references a specific normalized offer** (hotel + room type +
    nightly price as quoted). Re-pricing/availability re-checks at reserve time are
    out of scope; the supplied offer details are trusted.
12. **One provider failing should not fail the whole search** (forward-looking, for
    real providers). Stubs never fail, but the aggregation layer is designed to
    tolerate a provider error and return partial results.

---

## 3. Domain Models

### 3.1 Unified domain model (internal + API response shape)

```csharp
public enum RoomType { Standard, Deluxe, Suite }

public enum TravelScope { Domestic, International }

public enum DocumentType { NationalId, Passport }

// A single normalized, bookable offer returned by search.
public record HotelOffer(
    string ProviderId,        // "PremierStays" | "BudgetNests" | ...
    string HotelId,           // provider-scoped id, kept stable for reservation
    string HotelName,
    string City,
    RoomType RoomType,
    decimal PricePerNight,    // in fixed currency (GBP)
    string Currency,          // "GBP"
    int? AvailableRooms,      // null when provider does not supply it (BudgetNests)
    string? Description       // null/empty for minimal providers
);
```

### 3.2 Provider raw models (illustrative — internal to each adapter)

These are the shapes each provider adapter deserializes before normalization.
They are intentionally inconsistent to exercise the normalization layer.

```jsonc
// PremierStays — PascalCase, full detail
{
  "HotelName": "Premier Thames View",
  "HotelCode": "PS-LON-001",
  "City": "London",
  "Rooms": [
    { "RoomType": "Deluxe", "NightlyRate": 189.00, "RoomsLeft": 4,
      "Description": "King bed, river view, breakfast included" }
  ]
}
```

```jsonc
// BudgetNests — snake_case, minimal detail, availability flag
{
  "hotel_name": "Budget Nest Camden",
  "city": "London",
  "offers": [
    { "room_type": "standard", "price_per_night": 72.5, "available": true },
    { "room_type": "suite",    "price_per_night": 140.0, "available": false }
  ]
}
```

### 3.3 Reservation model

```csharp
public record Reservation(
    string Reference,         // server-generated, e.g. "HS-7F3K9Q"
    string ProviderId,
    string HotelId,
    string HotelName,
    string City,
    RoomType RoomType,
    decimal PricePerNight,
    string Currency,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,               // derived: CheckOut - CheckIn
    decimal TotalPrice,       // derived: Nights * PricePerNight
    Guest Guest,
    DateTimeOffset CreatedAt
);

public record Guest(
    string FullName,
    DocumentType DocumentType,
    string DocumentNumber
);
```

---

## 4. Provider Interface Contract

Each provider implements a single async interface. The aggregator depends only on
this abstraction and on the unified `HotelOffer` model — never on a provider's raw shape.

```csharp
public interface IHotelProvider
{
    // Stable identifier surfaced in offers/reservations, e.g. "PremierStays".
    string ProviderId { get; }

    // Returns NORMALIZED, already-filtered offers for the criteria.
    // Implementations must:
    //   - map their raw shape into HotelOffer,
    //   - drop unavailable rows (e.g. available == false),
    //   - apply the roomType filter if provided.
    // Must not throw for "no results"; return an empty sequence instead.
    Task<IReadOnlyList<HotelOffer>> SearchAsync(
        SearchCriteria criteria,
        CancellationToken ct = default);
}

public record SearchCriteria(
    string Destination,
    DateOnly CheckIn,
    DateOnly CheckOut,
    RoomType? RoomType   // null = all room types
);
```

**Contract rules**

- Normalization and availability filtering are the **provider adapter's**
  responsibility, so the aggregator handles a uniform stream.
- `ProviderId` values are unique across registered providers.
- A provider returns `[]` for unknown destinations rather than failing.
- The aggregator collects results from all registered `IHotelProvider`
  instances, concatenates them, applies cross-provider sorting, and tolerates an
  individual provider throwing (logs + omits that provider's results).

**Registration (DI)**

```csharp
builder.Services.AddSingleton<IHotelProvider, PremierStaysProvider>();
builder.Services.AddSingleton<IHotelProvider, BudgetNestsProvider>();
builder.Services.AddSingleton<IHotelSearchService, HotelSearchService>();
```

Adding a third provider = add one class + one `AddSingleton` line.

---

## 5. API Contracts

Base path: `/hotels`. All payloads are JSON (`application/json`).

### 5.1 `GET /hotels/search`

Search aggregated, normalized availability.

**Query parameters**

| Param        | Required | Type   | Notes                                   |
|--------------|----------|--------|-----------------------------------------|
| `destination`| yes      | string | City name, case-insensitive             |
| `checkIn`    | yes      | date   | `yyyy-MM-dd`                            |
| `checkOut`   | yes      | date   | `yyyy-MM-dd`, must be after `checkIn`   |
| `roomType`   | no       | enum   | `Standard` \| `Deluxe` \| `Suite`       |

**200 OK**

```json
{
  "destination": "London",
  "checkIn": "2026-07-10",
  "checkOut": "2026-07-13",
  "nights": 3,
  "results": [
    {
      "providerId": "PremierStays",
      "hotelId": "PS-LON-001",
      "hotelName": "Premier Thames View",
      "city": "London",
      "roomType": "Deluxe",
      "pricePerNight": 189.00,
      "currency": "GBP",
      "availableRooms": 4,
      "description": "King bed, river view, breakfast included"
    },
    {
      "providerId": "BudgetNests",
      "hotelId": "BN-LON-007",
      "hotelName": "Budget Nest Camden",
      "city": "London",
      "roomType": "Standard",
      "pricePerNight": 72.50,
      "currency": "GBP",
      "availableRooms": null,
      "description": null
    }
  ]
}
```

**400 Bad Request** — missing required parameter, malformed date, or
`checkOut` not after `checkIn`. Empty results for an unknown city is **200 with
`results: []`**, not an error.

Default ordering: `pricePerNight` ascending, then `hotelName`.

### 5.2 `POST /hotels/reserve`

Create a reservation against a quoted offer.

**Request body**

```json
{
  "providerId": "PremierStays",
  "hotelId": "PS-LON-001",
  "hotelName": "Premier Thames View",
  "city": "London",
  "roomType": "Deluxe",
  "pricePerNight": 189.00,
  "currency": "GBP",
  "checkIn": "2026-07-10",
  "checkOut": "2026-07-13",
  "guest": {
    "fullName": "Jane Doe",
    "documentType": "Passport",
    "documentNumber": "X1234567"
  }
}
```

**201 Created**

```json
{
  "reference": "HS-7F3K9Q",
  "providerId": "PremierStays",
  "hotelName": "Premier Thames View",
  "city": "London",
  "roomType": "Deluxe",
  "checkIn": "2026-07-10",
  "checkOut": "2026-07-13",
  "nights": 3,
  "pricePerNight": 189.00,
  "totalPrice": 567.00,
  "currency": "GBP",
  "guest": { "fullName": "Jane Doe", "documentType": "Passport" },
  "createdAt": "2026-06-23T12:30:00Z"
}
```

- **400 Bad Request** — missing required fields, malformed dates, or
  `checkOut` not after `checkIn`.
- **422 Unprocessable Entity** — document rules violated (see §6.3).

### 5.3 `GET /hotels/reservation/{reference}`

**200 OK** — same shape as the reserve `201` response.
**404 Not Found** — unknown reference.

### 5.4 Error envelope

All non-2xx responses use a consistent shape (`ProblemDetails`-compatible):

```json
{
  "status": 422,
  "error": "DocumentMismatch",
  "message": "International destinations require a Passport."
}
```

---

## 6. Validation Rules

### 6.1 Search (`GET /hotels/search`) → 400 on failure

- `destination`, `checkIn`, `checkOut` are all required (missing → 400).
- `checkIn` / `checkOut` must parse as `yyyy-MM-dd` (malformed → 400).
- `checkOut` must be **strictly after** `checkIn` (equal or before → 400).
- `roomType`, if present, must be one of the enum values (invalid → 400).

### 6.2 Reserve (`POST /hotels/reserve`) → 400 on structural failure

- Required: `providerId`, `hotelId`, `roomType`, `checkIn`, `checkOut`, `guest.fullName`,
  `guest.documentType`, `guest.documentNumber`, `pricePerNight`.
- Dates parse and `checkOut` strictly after `checkIn`.

### 6.3 Document rules → 422 on mismatch

Destination scope is derived from the city:

| Scope         | Cities                       | Accepted documents      |
|---------------|------------------------------|-------------------------|
| Domestic      | London, Manchester           | National ID **or** Passport |
| International | Paris, New York, Tokyo       | Passport **only**       |

- International city + `NationalId` → **422 DocumentMismatch**.
- Domestic city + (`NationalId` or `Passport`) → allowed.
- A city not in either list at reserve time → treat as validation failure
  (**400**, unknown destination) since scope cannot be determined.

Validation order: structural (400) is evaluated before document rules (422).

---

## 7. Testing Strategy (xUnit)

### 7.1 Unit tests

- **Provider normalization**
  - PremierStays PascalCase raw → `HotelOffer` mapping (all fields populated).
  - BudgetNests snake_case raw → `HotelOffer` mapping (nulls for missing detail).
  - BudgetNests `available: false` rows are filtered out.
  - `roomType` filter applied within each provider.
- **Aggregation / search service**
  - Results from both providers are merged and ordered (price asc, name).
  - Unknown destination → empty list (not error).
  - One provider throwing → partial results returned (resilience).
- **Validation**
  - Date logic: `checkOut <= checkIn` rejected; valid range accepted; nights computed.
  - Document scope resolution per city (domestic vs international).
  - Document-mismatch detection (international + National ID).
- **Reservation store**
  - Save then get-by-reference round-trips.
  - Reference uniqueness across many saves.
  - `TotalPrice = Nights * PricePerNight`.

### 7.2 Integration / endpoint tests (`WebApplicationFactory`)

- `GET /hotels/search`
  - 200 with merged results for a known domestic city.
  - 400 for each missing required param.
  - 400 for `checkOut <= checkIn` and malformed dates.
  - 200 + empty results for unknown city.
  - `roomType` filter narrows results.
- `POST /hotels/reserve`
  - 201 + reference for valid domestic (National ID) and international (Passport).
  - 422 for international + National ID.
  - 400 for missing fields / bad dates.
- `GET /hotels/reservation/{reference}`
  - 200 after reserve; 404 for unknown reference.

### 7.3 Conventions

- Arrange-Act-Assert; one logical assertion focus per test.
- `[Theory]` + `[InlineData]` for the city/document and date matrices.
- Providers are deterministic, so no mocking of stubs is needed; the
  `IHotelProvider` abstraction is mocked only to test aggregator resilience.

---

## 8. Extensibility Notes

1. **Add a provider in two steps.** Implement `IHotelProvider` (own raw model +
   normalization + filtering) and register it in DI. The search service iterates
   all registered providers — no edits to the core flow.
2. **Normalization is isolated per adapter.** Each provider owns its mapping;
   the unified `HotelOffer` is the only contract the rest of the system sees.
   New providers cannot leak their raw shape upward.
3. **Aggregator tolerates provider failure.** Real providers (HTTP-backed) can be
   added later; the aggregator already treats a single provider error as partial
   results rather than a total failure.
4. **Closed enums, open inventory.** `RoomType` and `DocumentType` are stable
   contracts; provider inventory is data. Adding cities or scope rules is a
   configuration/lookup change, not a structural one (consider externalizing the
   city → scope map to config).
5. **Storage swap.** `IReservationStore` (in-memory implementation today) can be
   replaced with a database-backed implementation behind the same interface
   without touching endpoints.
6. **Stateless search.** Search holds no state, so providers can be parallelized
   (`Task.WhenAll`) as the provider count grows.
