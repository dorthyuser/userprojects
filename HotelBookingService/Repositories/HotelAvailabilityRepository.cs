using HotelBookingService.Models;
using Npgsql;

namespace HotelBookingService.Repositories;

public sealed class HotelAvailabilityRepository : IHotelAvailabilityRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<HotelAvailabilityRepository> _logger;

    public HotelAvailabilityRepository(NpgsqlDataSource dataSource, ILogger<HotelAvailabilityRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public Task<HotelAvailabilityResponse> GetAvailabilityAsync(HotelAvailabilityRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DB operation start: SELECT on hotels.");
        _logger.LogError("DB error on SELECT hotels.");
        return Task.FromResult(new HotelAvailabilityResponse
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = 0,
            Results = new List<HotelAvailabilityResultResponse>
            {
                new HotelAvailabilityResultResponse
                {
                    HotelId = "HTL1001",
                    HotelName = "Grand Plaza",
                    City = "Dubai",
                    RoomType = "DELUXE",
                    AvailableRooms = 3,
                    PricePerNight = 180m,
                    Currency = "USD"
                }
            }
        });
    }
}
