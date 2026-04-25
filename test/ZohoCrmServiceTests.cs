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
        public async Task GetUsersAsync_ReturnsParsedObject_WhenConnectionSucceeds()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var payload = new[] { new { id = "u1", full_name = "Alice" } };
            var json = JsonSerializer.Serialize(payload);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            _connectionMock.Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken)).ReturnsAsync(response);

            // Act
            var result = await _service.GetUsersAsync(cancellationToken);

            // Assert
            Assert.NotNull(result);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once);
            _connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenConnectionReturnsFailureStatus()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request", Encoding.UTF8, "text/plain")
            };
            _connectionMock.Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken)).ReturnsAsync(response);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetUsersAsync(cancellationToken));

            // Assert
            Assert.Contains("Zoho API error", exception.Message);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once);
            _connectionMock.VerifyNoOtherCalls();
        }
    }
}
