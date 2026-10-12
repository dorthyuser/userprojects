using HotelBookingService.Models;

namespace HotelBookingService.Services;

public interface IBookingService
{
    Task<CreateBookingResponse?> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken);
    Task<GetBookingResponse?> GetBookingAsync(string bookingId, CancellationToken cancellationToken);
    Task<CancelBookingResponse?> CancelBookingAsync(string bookingId, CancelBookingRequest request, CancellationToken cancellationToken);
}
