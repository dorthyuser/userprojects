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
using ZohoProject2.Models;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class ZohoCrmServiceTests
    {
        [Fact]
        public async Task GetUsersAsync_ReturnsParsedResult_OnSuccess()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var expectedPayload = new[] { new { id = "u1", name = "Jane" } };
            var json = JsonSerializer.Serialize(expectedPayload);
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.GetUsersAsync(cancellationToken);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_OnNonSuccessStatus()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var errorBody = "bad request";

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(errorBody, Encoding.UTF8, "text/plain")
                });

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync(cancellationToken));

            // Assert
            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsParsedResult_OnSuccess()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var request = new CreateUserRequest();
            var expectedPayload = new[] { new { id = "u2", name = "Created" } };
            var json = JsonSerializer.Serialize(expectedPayload);
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.CreateUserAsync(request, cancellationToken);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsInvalidOperationException_OnNonSuccessStatus()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var request = new CreateUserRequest();
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("server error", Encoding.UTF8, "text/plain")
                });

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(request, cancellationToken));

            // Assert
            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsParsedResult_OnSuccess()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var id = "123";
            var request = new UpdateUserRequest();
            var expectedPayload = new[] { new { id = "123", status = "updated" } };
            var json = JsonSerializer.Serialize(expectedPayload);
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string?>(), cancellationToken))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.UpdateUserAsync(id, request, cancellationToken);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string?>(), cancellationToken), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsInvalidOperationException_OnNonSuccessStatus()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var id = "123";
            var request = new UpdateUserRequest();
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string?>(), cancellationToken))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("not found", Encoding.UTF8, "text/plain")
                });

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUserAsync(id, request, cancellationToken));

            // Assert
            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string?>(), cancellationToken), Times.Once());
        }
    }
}
