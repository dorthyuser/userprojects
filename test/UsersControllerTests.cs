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
        private readonly Mock<IZohoCrmService> _serviceMock;
        private readonly Mock<ILogger<UsersController>> _loggerMock;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<UsersController>>();
            _controller = new UsersController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetUsers_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var expected = new[] { new { id = "u1", name = "Alice" } };
            _serviceMock.Setup(s => s.GetUsersAsync(cancellationToken)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetUsers(cancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(expected, okResult.Value);
            _serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once);
            _serviceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsers_ReturnsInternalServerError_WhenServiceThrows()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var exceptionMessage = "boom";
            _serviceMock.Setup(s => s.GetUsersAsync(cancellationToken)).ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _controller.GetUsers(cancellationToken);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.NotNull(objectResult.Value);
            _serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once);
            _serviceMock.VerifyNoOtherCalls();
        }
    }
}
