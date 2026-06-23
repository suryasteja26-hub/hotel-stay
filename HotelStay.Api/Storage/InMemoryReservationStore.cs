using System.Collections.Concurrent;
using HotelStay.Api.Domain;

namespace HotelStay.Api.Storage;

/// <summary>
/// Thread-safe, process-lifetime reservation store. Not durable — all data is
/// lost on restart. Swap for a database-backed store behind the same interface.
/// </summary>
public sealed class InMemoryReservationStore : IReservationStore
{
    private readonly ConcurrentDictionary<string, ReservationResponse> _reservations =
        new(StringComparer.OrdinalIgnoreCase);

    public void Save(ReservationResponse reservation) =>
        _reservations[reservation.Reference] = reservation;

    public ReservationResponse? Get(string reference) =>
        _reservations.TryGetValue(reference, out var reservation) ? reservation : null;
}
