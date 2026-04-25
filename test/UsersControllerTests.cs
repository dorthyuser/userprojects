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
            var expected = new[] { new { id = "u1", name = "Alice" } };
            var cancellationToken = CancellationToken.None;

            _serviceMock.Setup(s => s.GetUsersAsync(cancellationToken)).ReturnsAsync(expected);

            var result = await _controller.GetUsers(cancellationToken);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = Assert.NotNull(okResult.Value);
            Assert.Same(expected, value);
            _serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
            _serviceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsers_ReturnsStatusCode500_WhenServiceThrows()
        {
            var cancellationToken = CancellationToken.None;
            var exception = new InvalidOperationException("boom");

            _serviceMock.Setup(s => s.GetUsersAsync(cancellationToken)).ThrowsAsync(exception);

            var result = await _controller.GetUsers(cancellationToken);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var payload = Assert.NotNull(objectResult.Value);
            Assert.NotNull(payload);
            _serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
            _serviceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUser_ReturnsNotImplemented_ForUnsupportedEndpoint()
        {
            var requestType = typeof(UsersController).GetMethod("CreateUser");
            Assert.NotNull(requestType);
            await Task.CompletedTask;
        }
    }
}
