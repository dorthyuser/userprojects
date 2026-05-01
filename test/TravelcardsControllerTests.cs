// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using new_test_for_demo.Controllers;
using new_test_for_demo.Models;
using new_test_for_demo.Services;
using Xunit;

namespace new_test_for_demo.Tests;

public class TravelcardsControllerTests
{
    [Fact]
    public async Task Create_ReturnsBadRequest_WhenClientIdIsNullOrWhitespace()
    {
        var serviceMock = new Mock<ITravelcardService>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<TravelcardsController>>();
        var controller = new TravelcardsController(serviceMock.Object, loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = BuildValidRequest();

        var result = await controller.Create(string.Empty, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenClientIdIsInvalid()
    {
        var serviceMock = new Mock<ITravelcardService>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<TravelcardsController>>();
        var controller = new TravelcardsController(serviceMock.Object, loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = BuildValidRequest();

        var result = await controller.Create("bad-id!", request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenContentTypeIsNotJson()
    {
        var serviceMock = new Mock<ITravelcardService>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<TravelcardsController>>();
        var controller = new TravelcardsController(serviceMock.Object, loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["Content-Type"] = "text/plain";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        var request = BuildValidRequest();

        var result = await controller.Create("client123", request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
        serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ReturnsOk_WhenRequestIsValid()
    {
        var serviceMock = new Mock<ITravelcardService>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<TravelcardsController>>();
        var controller = new TravelcardsController(serviceMock.Object, loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["Content-Type"] = "application/json";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        var request = BuildValidRequest();
        var response = new CreateTravelcardResponse();
        serviceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateTravelcardRequest>()))
            .ReturnsAsync(response);

        var result = await controller.Create("client123", request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, okResult.Value);
        serviceMock.Verify(s => s.CreateAsync(It.IsAny<CreateTravelcardRequest>()), Times.Once());
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenServiceThrowsException()
    {
        var serviceMock = new Mock<ITravelcardService>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<TravelcardsController>>();
        var controller = new TravelcardsController(serviceMock.Object, loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Headers["Content-Type"] = "application/json";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        var request = BuildValidRequest();
        serviceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateTravelcardRequest>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.Create("client123", request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        serviceMock.Verify(s => s.CreateAsync(It.IsAny<CreateTravelcardRequest>()), Times.Once());
    }

    private static CreateTravelcardRequest BuildValidRequest()
    {
        return new CreateTravelcardRequest
        {
            TravelcardType = TravelcardTypeEnum.Young,
            TravelcardRequestedDate = DateTimeOffset.UtcNow.AddDays(-2),
            TravelcardValidFrom = DateTimeOffset.UtcNow.AddDays(-1),
            TravelcardValidTo = DateTimeOffset.UtcNow.AddDays(10),
            TravelcardUsableTo = DateTimeOffset.UtcNow.AddDays(10),
            Cardholders = new List<CardholderRequest>
            {
                new CardholderRequest
                {
                    CardholderType = CardholderTypeEnum.Primary,
                    CardholderPhotoKey = "photo-key"
                }
            }
        };
    }
}