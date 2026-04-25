// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class ZohoCrmServiceTests
    {
        [Fact]
        public async Task GetUsersAsync_ReturnsParsedObject_WhenResponseIsSuccessful()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var responseJson = JsonSerializer.Serialize(new { users = new[] { new { id = "u1", name = "Alice" } } });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>(MockBehavior.Loose);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.GetUsersAsync(cancellationToken);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenResponseFails()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request", Encoding.UTF8, "text/plain")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>(MockBehavior.Loose);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync(cancellationToken));
            Assert.Contains("Zoho API error", exception.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsParsedObject_WhenResponseIsSuccessful()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var request = new ZohoProject2.Models.CreateUserRequest();
            var responseJson = JsonSerializer.Serialize(new { id = "u1", status = "created" });
            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>(MockBehavior.Loose);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.CreateUserAsync(request, cancellationToken);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsInvalidOperationException_WhenResponseFails()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var request = new ZohoProject2.Models.CreateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("server error", Encoding.UTF8, "text/plain")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>(MockBehavior.Loose);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(request, cancellationToken));
            Assert.Contains("Zoho API error", exception.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsParsedObject_WhenResponseIsSuccessful()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var request = new ZohoProject2.Models.UpdateUserRequest();
            var responseJson = JsonSerializer.Serialize(new { id = "u1", status = "updated" });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>(MockBehavior.Loose);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u1", It.IsAny<string?>(), cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.UpdateUserAsync("u1", request, cancellationToken);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u1", It.IsAny<string?>(), cancellationToken), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsInvalidOperationException_WhenResponseFails()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var request = new ZohoProject2.Models.UpdateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("not found", Encoding.UTF8, "text/plain")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>(MockBehavior.Loose);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u1", It.IsAny<string?>(), cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUserAsync("u1", request, cancellationToken));
            Assert.Contains("Zoho API error", exception.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u1", It.IsAny<string?>(), cancellationToken), Times.Once());
        }
    }
}