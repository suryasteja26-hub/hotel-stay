using HotelStay.Api.Domain;

namespace HotelStay.Api.Storage;

public interface IReservationStore
{
    void Save(ReservationResponse reservation);

    ReservationResponse? Get(string reference);
}
