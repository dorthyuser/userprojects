using HotelBookingService.Models;
using HotelBookingService.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HotelBookingService.Tests;

public sealed class HotelAvailabilityRepositoryTests
{
    [Fact]
    public async Task GetAvailabilityAsync_ReturnsMappedResponse()
    {
        var logger = new Mock<ILogger<HotelAvailabilityRepository>>(MockBehavior.Loose);
        var repository = new HotelAvailabilityRepository(null!, logger.Object);
        var request = new HotelAvailabilityRequest
        {
            Page = 2,
            PageSize = 10,
            CheckInDate = DateOnly.Parse("2026-02-10"),
            CheckOutDate = DateOnly.Parse("2026-02-13")
        };

        var result = await repository.GetAvailabilityAsync(request, CancellationToken.None);

        Assert.Equal(request.Page, result.Page);
        Assert.Equal(request.PageSize, result.PageSize);
        Assert.Equal(0, result.TotalRecords);
        Assert.Single(result.Results);
        Assert.Equal("HTL1001", result.Results[0].HotelId);
        Assert.Equal("Grand Plaza", result.Results[0].HotelName);
        Assert.Equal("Dubai", result.Results[0].City);
        Assert.Equal("DELUXE", result.Results[0].RoomType);
        Assert.Equal(3, result.Results[0].AvailableRooms);
        Assert.Equal(180m, result.Results[0].PricePerNight);
        Assert.Equal("USD", result.Results[0].Currency);
    }
}
