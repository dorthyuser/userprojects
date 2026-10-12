using HotelBookingService.Models;

namespace HotelBookingService.Services;

public interface IHotelAvailabilityService
{
    Task<HotelAvailabilityResponse> GetAvailabilityAsync(HotelAvailabilityRequest request, CancellationToken cancellationToken);
}
