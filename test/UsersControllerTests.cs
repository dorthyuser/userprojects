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
        public async Task GetUsers_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<UsersController>>();
            var expectedResult = new { users = new[] { new { id = "1", name = "Jane" } } };
            var cancellationToken = CancellationToken.None;

            serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ReturnsAsync(expectedResult);

            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            // Act
            var result = await controller.GetUsers(cancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(expectedResult, okResult.Value);
            serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
        }

        [Fact]
        public async Task GetUsers_ReturnsInternalServerError_WhenServiceThrows()
        {
            // Arrange
            var serviceMock = new Mock<IZohoCrmService>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<UsersController>>();
            var cancellationToken = CancellationToken.None;
            var expectedException = new Exception("boom");

            serviceMock
                .Setup(s => s.GetUsersAsync(cancellationToken))
                .ThrowsAsync(expectedException);

            var controller = new UsersController(serviceMock.Object, loggerMock.Object);

            // Act
            var result = await controller.GetUsers(cancellationToken);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);

            var value = Assert.IsType<Dictionary<string, object>>(ToDictionary(objectResult.Value));
            Assert.True(value.TryGetValue("error", out var errorValue));
            Assert.Equal("boom", errorValue as string);

            serviceMock.Verify(s => s.GetUsersAsync(cancellationToken), Times.Once());
        }

        private static Dictionary<string, object> ToDictionary(object? value)
        {
            if (value is Dictionary<string, object> dict)
            {
                return dict;
            }

            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (value == null)
            {
                return result;
            }

            foreach (var prop in value.GetType().GetProperties())
            {
                result[prop.Name] = prop.GetValue(value)!;
            }

            return result;
        }
    }
}
