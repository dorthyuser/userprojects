// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ZohoProject2.Controllers;
using ZohoProject2.Services;
using Xunit;

namespace ZohoProject2.Tests.Controllers
{
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetUsers_ReturnsOk_WhenServiceSucceeds()
        {
            var mockService = new Mock<IZohoCrmService>();
            var mockLogger = new Mock<ILogger<UsersController>>();
            var expected = new { id = "1", name = "Test User" };

            mockService
                .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var controller = new UsersController(mockService.Object, mockLogger.Object);

            var result = await controller.GetUsers(CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            mockService.Verify(s => s.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsers_Returns500_WhenServiceThrows()
        {
            var mockService = new Mock<IZohoCrmService>();
            var mockLogger = new Mock<ILogger<UsersController>>();
            var exceptionMessage = "boom";

            mockService
                .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            var controller = new UsersController(mockService.Object, mockLogger.Object);

            var result = await controller.GetUsers(CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.NotNull(objectResult.Value);
            mockService.Verify(s => s.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}