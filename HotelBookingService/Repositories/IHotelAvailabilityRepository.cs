using HotelBookingService.Models;

namespace HotelBookingService.Repositories;

public interface IHotelAvailabilityRepository
{
    Task<HotelAvailabilityResponse> GetAvailabilityAsync(HotelAvailabilityRequest request, CancellationToken cancellationToken);
}
