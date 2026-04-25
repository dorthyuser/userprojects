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
        public async Task GetUsersAsync_ReturnsParsedResult_WhenConnectionSucceeds()
        {
            // Arrange
            var payload = JsonSerializer.Serialize(new { users = new[] { new { id = "u1", name = "Alice" } } });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var token = CancellationToken.None;
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, logger.Object);

            // Act
            var result = await sut.GetUsersAsync(token);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, token), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenConnectionReturnsError()
        {
            // Arrange
            var payload = JsonSerializer.Serialize(new { message = "error" });
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var token = CancellationToken.None;
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, logger.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetUsersAsync(token));

            // Assert
            Assert.Contains("Zoho API error", exception.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, token), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsParsedResult_WhenConnectionSucceeds()
        {
            // Arrange
            var request = new CreateUserRequest();
            var payload = JsonSerializer.Serialize(new { id = "u2" });
            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var token = CancellationToken.None;
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, logger.Object);

            // Act
            var result = await sut.CreateUserAsync(request, token);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), token), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsInvalidOperationException_WhenConnectionReturnsError()
        {
            // Arrange
            var request = new CreateUserRequest();
            var payload = JsonSerializer.Serialize(new { message = "bad request" });
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var token = CancellationToken.None;
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, logger.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateUserAsync(request, token));

            // Assert
            Assert.Contains("Zoho API error", exception.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), token), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsParsedResult_WhenConnectionSucceeds()
        {
            // Arrange
            var request = new UpdateUserRequest();
            var payload = JsonSerializer.Serialize(new { updated = true });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var token = CancellationToken.None;
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, logger.Object);

            // Act
            var result = await sut.UpdateUserAsync("123", request, token);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), token), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsInvalidOperationException_WhenConnectionReturnsError()
        {
            // Arrange
            var request = new UpdateUserRequest();
            var payload = JsonSerializer.Serialize(new { message = "not found" });
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var token = CancellationToken.None;
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, logger.Object);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateUserAsync("123", request, token));

            // Assert
            Assert.Contains("Zoho API error", exception.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), token), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }
    }
}