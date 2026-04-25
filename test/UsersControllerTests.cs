// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;
using zoho_project_csharp.Controllers;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Tests
{
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetUsers_ReturnsContentResult_OnSuccess()
        {
            // Arrange
            var mockService = new Mock<IZohoCrmService>();
            var mockLogger = new Mock<ILogger<UsersController>>();
            var users = new[] { new { id = "u1" } };
            var jsonString = JsonSerializer.Serialize(users);
            mockService.Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync((200, jsonString));
            var controller = new UsersController(mockService.Object, mockLogger.Object);

            // Act
            var actionResult = await controller.GetUsers(CancellationToken.None);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(actionResult);
            var status = contentResult.StatusCode;
            var content = contentResult.Content;
            Assert.Equal(200, status);
            Assert.Equal(jsonString, content);
            mockService.Verify(m => m.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsers_Returns500_OnException()
        {
            // Arrange
            var mockService = new Mock<IZohoCrmService>();
            var mockLogger = new Mock<ILogger<UsersController>>();
            mockService.Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("boom"));
            var controller = new UsersController(mockService.Object, mockLogger.Object);

            // Act
            var actionResult = await controller.GetUsers(CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(actionResult);
            var status = objectResult.StatusCode;
            Assert.Equal(500, status);
            var value = objectResult.Value;
            var prop = value.GetType().GetProperty("error");
            var errorValue = prop.GetValue(value);
            Assert.Equal("boom", errorValue);
            mockService.Verify(m => m.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
