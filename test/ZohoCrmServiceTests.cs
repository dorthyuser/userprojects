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
        public async Task GetUsersAsync_ReturnsParsedJson_WhenConnectionSucceeds()
        {
            // Arrange
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var cancellationToken = CancellationToken.None;
            var responseJson = JsonSerializer.Serialize(new { users = new[] { new { id = "1" } } });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.GetUsersAsync(cancellationToken);

            // Assert
            var resultText = JsonSerializer.Serialize(result);
            Assert.Contains("users", resultText);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenConnectionReturnsFailure()
        {
            // Arrange
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var cancellationToken = CancellationToken.None;
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request", Encoding.UTF8, "text/plain")
            };

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync(cancellationToken));
            var message = exception.Message;

            // Assert
            Assert.Contains("Zoho API error", message);
            Assert.Contains("400", message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsValue_WhenCalled()
        {
            // Arrange
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var cancellationToken = CancellationToken.None;
            var request = new CreateUserRequest();
            var responseJson = JsonSerializer.Serialize(new { created = true });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.CreateUserAsync(request, cancellationToken);

            // Assert
            var serialized = JsonSerializer.Serialize(result);
            Assert.Contains("created", serialized);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), cancellationToken), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_Throws_WhenConnectionFails()
        {
            // Arrange
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var cancellationToken = CancellationToken.None;
            var request = new UpdateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("error", Encoding.UTF8, "text/plain")
            };

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUserAsync("123", request, cancellationToken));
            var message = exception.Message;

            // Assert
            Assert.Contains("Zoho API error", message);
            Assert.Contains("500", message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), cancellationToken), Times.Once());
        }
    }
}
