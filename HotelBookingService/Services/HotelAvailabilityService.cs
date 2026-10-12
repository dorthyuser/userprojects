using System.ComponentModel.DataAnnotations;
using HotelBookingService.Models;
using HotelBookingService.Repositories;

namespace HotelBookingService.Services;

public sealed class HotelAvailabilityService : IHotelAvailabilityService
{
    private readonly IHotelAvailabilityRepository _hotelAvailabilityRepository;
    private readonly ILogger<HotelAvailabilityService> _logger;

    public HotelAvailabilityService(IHotelAvailabilityRepository hotelAvailabilityRepository, ILogger<HotelAvailabilityService> logger)
    {
        _hotelAvailabilityRepository = hotelAvailabilityRepository;
        _logger = logger;
    }

    public async Task<HotelAvailabilityResponse> GetAvailabilityAsync(HotelAvailabilityRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, context, results, true))
        {
            foreach (var result in results)
            {
                _logger.LogWarning("Validation failed for field {Field}: {Reason}", string.Join(",", result.MemberNames), result.ErrorMessage);
            }
            throw new ValidationException("Validation failed.");
        }

        if (request.CheckInDate >= request.CheckOutDate)
        {
            _logger.LogWarning("Validation failed for field checkOutDate: must be after checkInDate.");
            throw new ValidationException("Validation failed.");
        }

        _logger.LogInformation("Validation passed.");
        return await _hotelAvailabilityRepository.GetAvailabilityAsync(request, cancellationToken);
    }
}
