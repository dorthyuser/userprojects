using System.ComponentModel.DataAnnotations;
using HotelBookingService.Models;
using HotelBookingService.Repositories;
using HotelBookingService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HotelBookingService.Tests;

public sealed class BookingServiceTests
{
    [Fact]
    public async Task CreateBookingAsync_ReturnsResult_WhenValidationPasses()
    {
        var repository = new Mock<IBookingRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingService>>(MockBehavior.Loose);
        var request = new CreateBookingRequest
        {
            CustomerId = "CUST1",
            HotelId = "HTL1",
            RoomType = "DELUXE",
            CheckInDate = DateOnly.Parse("2026-02-10"),
            CheckOutDate = DateOnly.Parse("2026-02-13"),
            Guests = 2,
            Currency = "USD",
            IdempotencyKey = "idem-123"
        };
        var expected = new CreateBookingResponse { BookingId = "BKG1" };

        repository
            .Setup(x => x.CreateBookingAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new BookingService(repository.Object, logger.Object);

        var result = await service.CreateBookingAsync(request, CancellationToken.None);

        Assert.Same(expected, result);
        repository.VerifyAll();
    }

    [Fact]
    public async Task CreateBookingAsync_ThrowsValidationException_WhenCheckOutDateIsNotAfterCheckInDate()
    {
        var repository = new Mock<IBookingRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingService>>(MockBehavior.Loose);
        var request = new CreateBookingRequest
        {
            CustomerId = "CUST1",
            HotelId = "HTL1",
            RoomType = "DELUXE",
            CheckInDate = DateOnly.Parse("2026-02-10"),
            CheckOutDate = DateOnly.Parse("2026-02-10"),
            Guests = 2,
            Currency = "USD",
            IdempotencyKey = "idem-123"
        };

        var service = new BookingService(repository.Object, logger.Object);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateBookingAsync(request, CancellationToken.None));

        Assert.Equal("Validation failed.", ex.Message);
    }

    [Fact]
    public async Task GetBookingAsync_ReturnsNull_WhenBookingIdIsWhitespace()
    {
        var repository = new Mock<IBookingRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingService>>(MockBehavior.Loose);
        var service = new BookingService(repository.Object, logger.Object);

        var result = await service.GetBookingAsync(" ", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBookingAsync_ReturnsResult_WhenBookingIdIsValid()
    {
        var repository = new Mock<IBookingRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingService>>(MockBehavior.Loose);
        var expected = new GetBookingResponse { BookingId = "BKG1" };

        repository
            .Setup(x => x.GetBookingAsync("BKG1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new BookingService(repository.Object, logger.Object);

        var result = await service.GetBookingAsync("BKG1", CancellationToken.None);

        Assert.Same(expected, result);
        repository.VerifyAll();
    }

    [Fact]
    public async Task CancelBookingAsync_ReturnsNull_WhenBookingIdIsWhitespace()
    {
        var repository = new Mock<IBookingRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingService>>(MockBehavior.Loose);
        var service = new BookingService(repository.Object, logger.Object);
        var request = new CancelBookingRequest { CancelReason = "Changed plans" };

        var result = await service.CancelBookingAsync("", request, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CancelBookingAsync_ReturnsResult_WhenValidationPasses()
    {
        var repository = new Mock<IBookingRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingService>>(MockBehavior.Loose);
        var request = new CancelBookingRequest { CancelReason = "Changed plans" };
        var expected = new CancelBookingResponse { BookingId = "BKG1" };

        repository
            .Setup(x => x.CancelBookingAsync("BKG1", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new BookingService(repository.Object, logger.Object);

        var result = await service.CancelBookingAsync("BKG1", request, CancellationToken.None);

        Assert.Same(expected, result);
        repository.VerifyAll();
    }
}
