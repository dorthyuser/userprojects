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
        private readonly Mock<IZohoCrmConnection> _connectionMock;
        private readonly Mock<ILogger<ZohoCrmService>> _loggerMock;
        private readonly ZohoCrmService _service;

        public ZohoCrmServiceTests()
        {
            _connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<ZohoCrmService>>();
            _service = new ZohoCrmService(_connectionMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsParsedResult_WhenResponseIsSuccessful()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var json = JsonSerializer.Serialize(new { data = new[] { new { id = "1", name = "Alice" } } });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            _connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(response);

            // Act
            var result = await _service.GetUsersAsync(cancellationToken);

            // Assert
            Assert.NotNull(result);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenResponseIsUnsuccessful()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request", Encoding.UTF8, "text/plain")
            };
            _connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(response);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetUsersAsync(cancellationToken));

            // Assert
            Assert.Contains("Zoho API error", ex.Message);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsNotImplemented_WhenCalled()
        {
            // Arrange
            var request = new CreateUserRequest();
            var cancellationToken = CancellationToken.None;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { success = true }), Encoding.UTF8, "application/json")
            };
            _connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(response);

            // Act
            var result = await _service.CreateUserAsync(request, cancellationToken);

            // Assert
            Assert.NotNull(result);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsNotImplemented_WhenCalled()
        {
            // Arrange
            var request = new UpdateUserRequest();
            var cancellationToken = CancellationToken.None;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { success = true }), Encoding.UTF8, "application/json")
            };
            _connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/1", It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(response);

            // Act
            var result = await _service.UpdateUserAsync("1", request, cancellationToken);

            // Assert
            Assert.NotNull(result);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/1", It.IsAny<string>(), cancellationToken), Times.Once);
        }
    }
}