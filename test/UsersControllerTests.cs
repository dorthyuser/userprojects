// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZohoProject2.Controllers;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Controllers
{
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetUsers_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<UsersController>>();
            var cancellationToken = CancellationToken.None;
            var expected = new[] { new { id = "u1", name = "Alice" } };

            serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ReturnsAsync(expected);

            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            // Act
            var result = await controller.GetUsers(cancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Same(expected, okResult.Value);
            serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
            serviceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsers_ReturnsStatusCode500_WhenServiceThrows()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<UsersController>>();
            var cancellationToken = CancellationToken.None;
            var exception = new InvalidOperationException("boom");

            serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ThrowsAsync(exception);

            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            // Act
            var result = await controller.GetUsers(cancellationToken);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            dynamic payload = objectResult.Value!;
            Assert.Equal("boom", (string)payload.error);
            serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
            serviceMock.VerifyNoOtherCalls();
        }
    }
}
