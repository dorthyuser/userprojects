using HotelBookingService.Models;
using HotelBookingService.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HotelBookingService.Tests;

public sealed class BookingRepositoryTests
{
    [Fact]
    public async Task CreateBookingAsync_ReturnsMappedResponse()
    {
        var logger = new Mock<ILogger<BookingRepository>>(MockBehavior.Loose);
        var repository = new BookingRepository(null!, logger.Object);
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

        var result = await repository.CreateBookingAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("BKG30001", result!.BookingId);
        Assert.Equal("HB-20260210-0001", result.BookingNumber);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.Equal(request.CustomerId, result.CustomerId);
        Assert.Equal(request.HotelId, result.HotelId);
        Assert.Equal(request.RoomType, result.RoomType);
        Assert.Equal(request.CheckInDate, result.CheckInDate);
        Assert.Equal(request.CheckOutDate, result.CheckOutDate);
        Assert.Equal(request.Guests, result.Guests);
        Assert.Equal(540m, result.TotalAmount);
        Assert.Equal(request.Currency, result.Currency);
    }

    [Fact]
    public async Task GetBookingAsync_ReturnsMappedResponse()
    {
        var logger = new Mock<ILogger<BookingRepository>>(MockBehavior.Loose);
        var repository = new BookingRepository(null!, logger.Object);

        var result = await repository.GetBookingAsync("BKG1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("BKG1", result!.BookingId);
        Assert.Equal("HB-20260210-0001", result.BookingNumber);
        Assert.Equal("CUST901", result.CustomerId);
        Assert.Equal("HTL1001", result.HotelId);
        Assert.Equal("Grand Plaza", result.HotelName);
        Assert.Equal("DELUXE", result.RoomType);
        Assert.Equal(DateOnly.Parse("2026-02-10"), result.CheckInDate);
        Assert.Equal(DateOnly.Parse("2026-02-13"), result.CheckOutDate);
        Assert.Equal(2, result.Guests);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.Equal(540m, result.TotalAmount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task CancelBookingAsync_ReturnsMappedResponse()
    {
        var logger = new Mock<ILogger<BookingRepository>>(MockBehavior.Loose);
        var repository = new BookingRepository(null!, logger.Object);
        var request = new CancelBookingRequest { CancelReason = "Changed plans" };

        var result = await repository.CancelBookingAsync("BKG1", request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("BKG1", result!.BookingId);
        Assert.Equal(BookingStatus.Cancelled, result.Status);
        Assert.Equal(request.CancelReason, result.CancelReason);
    }
}
