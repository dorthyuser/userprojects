using System.ComponentModel.DataAnnotations;
using HotelBookingService.Models;
using HotelBookingService.Repositories;
using HotelBookingService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HotelBookingService.Tests;

public sealed class HotelAvailabilityServiceTests
{
    [Fact]
    public async Task GetAvailabilityAsync_ReturnsRepositoryResult_WhenValidationPasses()
    {
        var repository = new Mock<IHotelAvailabilityRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<HotelAvailabilityService>>(MockBehavior.Loose);
        var request = new HotelAvailabilityRequest
        {
            Page = 1,
            PageSize = 10,
            CheckInDate = DateOnly.Parse("2026-02-10"),
            CheckOutDate = DateOnly.Parse("2026-02-13"),
            Guests = 2
        };
        var expected = new HotelAvailabilityResponse();

        repository
            .Setup(x => x.GetAvailabilityAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new HotelAvailabilityService(repository.Object, logger.Object);

        var result = await service.GetAvailabilityAsync(request, CancellationToken.None);

        Assert.Same(expected, result);
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetAvailabilityAsync_ThrowsValidationException_WhenCheckOutDateIsNotAfterCheckInDate()
    {
        var repository = new Mock<IHotelAvailabilityRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<HotelAvailabilityService>>(MockBehavior.Loose);
        var request = new HotelAvailabilityRequest
        {
            Page = 1,
            PageSize = 10,
            CheckInDate = DateOnly.Parse("2026-02-10"),
            CheckOutDate = DateOnly.Parse("2026-02-10"),
            Guests = 2
        };

        var service = new HotelAvailabilityService(repository.Object, logger.Object);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.GetAvailabilityAsync(request, CancellationToken.None));

        Assert.Equal("Validation failed.", ex.Message);
    }
}
