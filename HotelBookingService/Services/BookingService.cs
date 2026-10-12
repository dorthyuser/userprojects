using System.ComponentModel.DataAnnotations;
using HotelBookingService.Models;
using HotelBookingService.Repositories;

namespace HotelBookingService.Services;

public sealed class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingService> _logger;

    public BookingService(IBookingRepository bookingRepository, ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<CreateBookingResponse?> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        Validate(request);
        _logger.LogInformation("Validation passed.");
        var result = await _bookingRepository.CreateBookingAsync(request, cancellationToken);
        if (result is not null)
        {
            _logger.LogInformation("Response sent. Identifier: {Identifier}", result.BookingId);
        }
        return result;
    }

    public async Task<GetBookingResponse?> GetBookingAsync(string bookingId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        if (string.IsNullOrWhiteSpace(bookingId))
        {
            _logger.LogWarning("Validation failed for field bookingId: required.");
            return null;
        }

        _logger.LogInformation("Validation passed.");
        var result = await _bookingRepository.GetBookingAsync(bookingId, cancellationToken);
        if (result is not null)
        {
            _logger.LogInformation("Response sent. Identifier: {Identifier}", result.BookingId);
        }
        return result;
    }

    public async Task<CancelBookingResponse?> CancelBookingAsync(string bookingId, CancelBookingRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating request...");
        if (string.IsNullOrWhiteSpace(bookingId))
        {
            _logger.LogWarning("Validation failed for field bookingId: required.");
            return null;
        }

        Validate(request);
        _logger.LogInformation("Validation passed.");
        var result = await _bookingRepository.CancelBookingAsync(bookingId, request, cancellationToken);
        if (result is not null)
        {
            _logger.LogInformation("Response sent. Identifier: {Identifier}", result.BookingId);
        }
        return result;
    }

    private void Validate(CreateBookingRequest request)
    {
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
    }

    private void Validate(CancelBookingRequest request)
    {
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
    }
}
