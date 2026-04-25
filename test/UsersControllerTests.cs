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
            var cancellationToken = new CancellationTokenSource().Token;
            var expected = new[] { new { id = "u1", name = "Alice" } };

            serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ReturnsAsync(expected);

            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            // Act
            var result = await controller.GetUsers(cancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(expected, okResult.Value);
            serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
            serviceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsers_ReturnsInternalServerError_WhenServiceThrows()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<UsersController>>();
            var cancellationToken = new CancellationTokenSource().Token;
            var exception = new Exception("boom");

            serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ThrowsAsync(exception);

            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            // Act
            var result = await controller.GetUsers(cancellationToken);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);

            Assert.NotNull(objectResult.Value);
            serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
            serviceMock.VerifyNoOtherCalls();
        }
    }
}