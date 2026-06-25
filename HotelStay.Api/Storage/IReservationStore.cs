using HotelStay.Api.Domain;

namespace HotelStay.Api.Storage;

public interface IReservationStore
{
    void Save(Reservation reservation);

    Reservation? Get(string reference);
}
