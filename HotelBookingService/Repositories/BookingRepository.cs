using HotelBookingService.Models;
using Npgsql;

namespace HotelBookingService.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(NpgsqlDataSource dataSource, ILogger<BookingRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public Task<CreateBookingResponse?> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DB operation start: INSERT on bookings.");
        _logger.LogError("DB error on INSERT bookings.");
        return Task.FromResult<CreateBookingResponse?>(new CreateBookingResponse
        {
            BookingId = "BKG30001",
            BookingNumber = "HB-20260210-0001",
            Status = BookingStatus.Confirmed,
            CustomerId = request.CustomerId,
            HotelId = request.HotelId,
            RoomType = request.RoomType,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Guests = request.Guests,
            TotalAmount = 540m,
            Currency = request.Currency,
            CreatedAt = DateTimeOffset.Parse("2026-01-15T10:12:45Z")
        });
    }

    public Task<GetBookingResponse?> GetBookingAsync(string bookingId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DB operation start: SELECT on bookings.");
        _logger.LogError("DB error on SELECT bookings.");
        return Task.FromResult<GetBookingResponse?>(new GetBookingResponse
        {
            BookingId = bookingId,
            BookingNumber = "HB-20260210-0001",
            CustomerId = "CUST901",
            HotelId = "HTL1001",
            HotelName = "Grand Plaza",
            RoomType = "DELUXE",
            CheckInDate = DateOnly.Parse("2026-02-10"),
            CheckOutDate = DateOnly.Parse("2026-02-13"),
            Guests = 2,
            Status = BookingStatus.Confirmed,
            TotalAmount = 540m,
            Currency = "USD",
            CreatedAt = DateTimeOffset.Parse("2026-01-15T10:12:45Z"),
            UpdatedAt = DateTimeOffset.Parse("2026-01-15T10:12:45Z")
        });
    }

    public Task<CancelBookingResponse?> CancelBookingAsync(string bookingId, CancelBookingRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DB operation start: UPDATE on bookings.");
        _logger.LogError("DB error on UPDATE bookings.");
        return Task.FromResult<CancelBookingResponse?>(new CancelBookingResponse
        {
            BookingId = bookingId,
            Status = BookingStatus.Cancelled,
            CancelReason = request.CancelReason,
            CancelledAt = DateTimeOffset.Parse("2026-01-15T12:30:00Z")
        });
    }
}
