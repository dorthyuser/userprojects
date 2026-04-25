// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZohoProject2.Controllers;
using ZohoProject2.Models;
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
            var expected = new[] { new { id = "1", name = "Alice" } };
            _serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetUsers(cancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(expected, okResult.Value);
            _serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetUsers_ReturnsInternalServerError_WhenServiceThrows()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var exception = new Exception("boom");
            _serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.GetUsers(cancellationToken);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            _serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateUser_ReturnsNotImplemented_WhenCalled()
        {
            // Arrange
            var body = new CreateUserRequest();
            _serviceMock
                .Setup(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new { });
            var method = typeof(UsersController).GetMethod(nameof(UsersController.CreateUser));
            Assert.NotNull(method);

            // Act
            var resultTask = (Task<IActionResult>)method!.Invoke(_controller, new object[] { body, CancellationToken.None })!;
            var result = await resultTask;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _serviceMock.Verify(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ReturnsNotImplemented_WhenCalledWithNullBody()
        {
            // Arrange
            var method = typeof(UsersController).GetMethod(nameof(UsersController.CreateUser));
            Assert.NotNull(method);

            // Act
            var resultTask = (Task<IActionResult>)method!.Invoke(_controller, new object?[] { null!, CancellationToken.None })!;
            var result = await resultTask;

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(501, objectResult.StatusCode);
            _serviceMock.VerifyNoOtherCalls();
        }
    }
}