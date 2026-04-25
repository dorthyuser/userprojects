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
            var cancellationToken = CancellationToken.None;
            var expected = new { users = new[] { new { id = "u1", name = "Alice" } } };
            _serviceMock.Setup(s => s.GetUsersAsync(cancellationToken)).ReturnsAsync(expected);

            var result = await _controller.GetUsers(cancellationToken);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            Assert.Same(expected, value);
            _serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
            _serviceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsers_ReturnsInternalServerError_WhenServiceThrows()
        {
            var cancellationToken = CancellationToken.None;
            var exception = new InvalidOperationException("boom");
            _serviceMock.Setup(s => s.GetUsersAsync(cancellationToken)).ThrowsAsync(exception);

            var result = await _controller.GetUsers(cancellationToken);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var statusCode = objectResult.StatusCode;
            var errorPayload = objectResult.Value;
            Assert.Equal(500, statusCode);
            Assert.NotNull(errorPayload);
            _serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
            _serviceMock.VerifyNoOtherCalls();
        }
    }
}
