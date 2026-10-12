using HotelBookingService.Controllers;
using HotelBookingService.Models;
using HotelBookingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HotelBookingService.Tests;

public sealed class HotelsControllerTests
{
    [Fact]
    public async Task GetAvailability_ReturnsOk_WhenServiceReturnsResult()
    {
        var service = new Mock<IHotelAvailabilityService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<HotelsController>>(MockBehavior.Loose);
        var request = new HotelAvailabilityRequest();
        var expected = new HotelAvailabilityResponse();

        service
            .Setup(x => x.GetAvailabilityAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new HotelsController(service.Object, logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetAvailability(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        service.VerifyAll();
    }
}
