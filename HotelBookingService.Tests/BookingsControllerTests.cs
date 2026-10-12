using HotelBookingService.Controllers;
using HotelBookingService.Models;
using HotelBookingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HotelBookingService.Tests;

public sealed class BookingsControllerTests
{
    [Fact]
    public async Task CreateBooking_ReturnsOk_WhenServiceReturnsResult()
    {
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingsController>>(MockBehavior.Loose);
        var request = new CreateBookingRequest();
        var expected = new CreateBookingResponse();

        bookingService
            .Setup(x => x.CreateBookingAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new BookingsController(bookingService.Object, logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.CreateBooking(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        bookingService.VerifyAll();
    }

    [Fact]
    public async Task CreateBooking_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingsController>>(MockBehavior.Loose);
        var request = new CreateBookingRequest();

        bookingService
            .Setup(x => x.CreateBookingAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateBookingResponse?)null);

        var controller = new BookingsController(bookingService.Object, logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.CreateBooking(request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        bookingService.VerifyAll();
    }

    [Fact]
    public async Task GetBooking_ReturnsOk_WhenServiceReturnsResult()
    {
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingsController>>(MockBehavior.Loose);
        var expected = new GetBookingResponse();

        bookingService
            .Setup(x => x.GetBookingAsync("BKG1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new BookingsController(bookingService.Object, logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetBooking("BKG1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        bookingService.VerifyAll();
    }

    [Fact]
    public async Task GetBooking_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingsController>>(MockBehavior.Loose);

        bookingService
            .Setup(x => x.GetBookingAsync("BKG1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetBookingResponse?)null);

        var controller = new BookingsController(bookingService.Object, logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetBooking("BKG1", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        bookingService.VerifyAll();
    }

    [Fact]
    public async Task CancelBooking_ReturnsOk_WhenServiceReturnsResult()
    {
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingsController>>(MockBehavior.Loose);
        var request = new CancelBookingRequest();
        var expected = new CancelBookingResponse();

        bookingService
            .Setup(x => x.CancelBookingAsync("BKG1", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new BookingsController(bookingService.Object, logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.CancelBooking("BKG1", request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        bookingService.VerifyAll();
    }

    [Fact]
    public async Task CancelBooking_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var bookingService = new Mock<IBookingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<BookingsController>>(MockBehavior.Loose);
        var request = new CancelBookingRequest();

        bookingService
            .Setup(x => x.CancelBookingAsync("BKG1", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancelBookingResponse?)null);

        var controller = new BookingsController(bookingService.Object, logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.CancelBooking("BKG1", request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        bookingService.VerifyAll();
    }
}
