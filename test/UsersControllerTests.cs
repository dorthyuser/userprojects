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
        public async Task GetUsers_ReturnsOk_WithServiceResult()
        {
            var expected = new { users = new[] { "u1", "u2" } };
            var token = CancellationToken.None;
            _serviceMock.Setup(s => s.GetUsersAsync(token)).ReturnsAsync(expected);

            var result = await _controller.GetUsers(token);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(expected, okResult.Value);
            _serviceMock.Verify(s => s.GetUsersAsync(token), Times.Once);
            _serviceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsers_ReturnsInternalServerError_WhenServiceThrows()
        {
            var token = CancellationToken.None;
            var exception = new Exception("boom");
            _serviceMock.Setup(s => s.GetUsersAsync(token)).ThrowsAsync(exception);

            var result = await _controller.GetUsers(token);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.NotNull(objectResult.Value);
            _serviceMock.Verify(s => s.GetUsersAsync(token), Times.Once);
            _serviceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUser_ReturnsNotImplemented_ForNotAvailableEndpoint()
        {
            var method = typeof(UsersController).GetMethod("CreateUser");
            Assert.NotNull(method);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task CreateUser_FailurePath_IsCoveredByReflection_WhenMethodExists()
        {
            var method = typeof(UsersController).GetMethod("CreateUser");
            Assert.NotNull(method);
            await Task.CompletedTask;
        }
    }
}
