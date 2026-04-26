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
        [Fact]
        public async Task GetUsers_ReturnsOk_WithServiceResult()
        {
            var cancellationToken = new CancellationTokenSource().Token;
            var expected = new { id = 1, name = "Alice" };
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ReturnsAsync(expected);
            var loggerMock = new Mock<ILogger<UsersController>>();
            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            var result = await controller.GetUsers(cancellationToken);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            Assert.Same(expected, value);
            serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
        }

        [Fact]
        public async Task GetUsers_ReturnsStatusCode500_WhenServiceThrows()
        {
            var cancellationToken = new CancellationTokenSource().Token;
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ThrowsAsync(new Exception("boom"));
            var loggerMock = new Mock<ILogger<UsersController>>();
            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            var result = await controller.GetUsers(cancellationToken);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var statusCode = objectResult.StatusCode;
            var statusCodeValue = statusCode.HasValue ? statusCode.Value : 0;
            Assert.Equal(500, statusCodeValue);
            serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching users")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once());
        }

        [Fact]
        public async Task CreateUser_ReturnsNotImplemented_WhenRequestIsNull()
        {
            var cancellationToken = new CancellationTokenSource().Token;
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<UsersController>>();
            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            var result = await controller.CreateUser(null!, cancellationToken);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(501, statusResult.StatusCode);
            serviceMock.Verify(s => s.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task CreateUser_ReturnsCreated_WhenRequestIsValid()
        {
            var cancellationToken = new CancellationTokenSource().Token;
            var request = new CreateUserRequest();
            var expected = new { id = "10", status = "created" };
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            serviceMock
                .Setup(s => s.CreateUserAsync(request, cancellationToken))
                .ReturnsAsync(expected);
            var loggerMock = new Mock<ILogger<UsersController>>();
            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            var result = await controller.CreateUser(request, cancellationToken);

            var createdResult = Assert.IsType<OkObjectResult>(result);
            var value = createdResult.Value;
            Assert.Same(expected, value);
            serviceMock.Verify(s => s.CreateUserAsync(request, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task CreateUser_ReturnsStatusCode500_WhenServiceThrows()
        {
            var cancellationToken = new CancellationTokenSource().Token;
            var request = new CreateUserRequest();
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            serviceMock
                .Setup(s => s.CreateUserAsync(request, cancellationToken))
                .ThrowsAsync(new Exception("create failed"));
            var loggerMock = new Mock<ILogger<UsersController>>();
            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            var result = await controller.CreateUser(request, cancellationToken);

            var objectResult = Assert.IsType<ObjectResult>(result);
            var statusCode = objectResult.StatusCode;
            var statusCodeValue = statusCode.HasValue ? statusCode.Value : 0;
            Assert.Equal(500, statusCodeValue);
            serviceMock.Verify(s => s.CreateUserAsync(request, cancellationToken), Times.Once());
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating user")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once());
        }
    }
}