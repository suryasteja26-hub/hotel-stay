using System.Collections.Concurrent;
using HotelStay.Api.Domain;

namespace HotelStay.Api.Storage;

/// <summary>
/// Thread-safe, process-lifetime reservation store. Not durable — all data is
/// lost on restart. Swap for a database-backed store behind the same interface.
/// </summary>
public sealed class InMemoryReservationStore : IReservationStore
{
    private readonly ConcurrentDictionary<string, Reservation> _reservations =
        new(StringComparer.OrdinalIgnoreCase);

    public void Save(Reservation reservation) =>
        _reservations[reservation.Reference] = reservation;

    public Reservation? Get(string reference) =>
        _reservations.TryGetValue(reference, out var reservation) ? reservation : null;
}
