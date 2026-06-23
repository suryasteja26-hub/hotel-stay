using HotelStay.Api.Domain;

namespace HotelStay.Api.Providers;

public interface IHotelProvider
{
    string Name { get; }

    Task<IReadOnlyList<HotelRoomOption>> SearchAsync(
        HotelSearchRequest request,
        CancellationToken cancellationToken);
}
