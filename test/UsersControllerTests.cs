// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Collections.Generic;
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
        public async Task GetUsers_ReturnsOkObjectResult_WhenServiceSucceeds()
        {
            var expected = new List<object>
            {
                new { id = "1", name = "Alice" }
            };

            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            serviceMock
                .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var loggerMock = new Mock<ILogger<UsersController>>();
            var sut = new UsersController(serviceMock.Object, loggerMock.Object);

            var result = await sut.GetUsers(CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.NotNull(actual);
            serviceMock.Verify(s => s.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsers_ReturnsStatusCode500_WhenServiceThrows()
        {
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            serviceMock
                .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));

            var loggerMock = new Mock<ILogger<UsersController>>();
            var sut = new UsersController(serviceMock.Object, loggerMock.Object);

            var result = await sut.GetUsers(CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.NotNull(objectResult.Value);
            serviceMock.Verify(s => s.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsers_ReturnsStatusCode500_WhenCancellationTokenIsCancelledAndServiceThrows()
        {
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            serviceMock
                .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("cancelled"));

            var loggerMock = new Mock<ILogger<UsersController>>();
            var sut = new UsersController(serviceMock.Object, loggerMock.Object);
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            var result = await sut.GetUsers(tokenSource.Token);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            serviceMock.Verify(s => s.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
