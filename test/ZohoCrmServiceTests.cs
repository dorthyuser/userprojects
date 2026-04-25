// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.IO;
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
        public async Task GetUsersAsync_ReturnsDeserializedResult_WhenApiSucceeds()
        {
            var payloadObject = new[] { new { id = "1", full_name = "Alice" } };
            var payload = JsonSerializer.Serialize(payloadObject);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var cancellationToken = CancellationToken.None;

            _connectionMock.Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken)).ReturnsAsync(response);

            var result = await _service.GetUsersAsync(cancellationToken);

            Assert.NotNull(result);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
            _connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsersAsync_Throws_WhenApiReturnsFailureStatus()
        {
            var body = "service unavailable";
            var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain")
            };
            var cancellationToken = CancellationToken.None;

            _connectionMock.Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken)).ReturnsAsync(response);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetUsersAsync(cancellationToken));

            Assert.Contains("Zoho API error", ex.Message);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
            _connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsValue_WhenApiSucceeds()
        {
            var request = new CreateUserRequest();
            var payloadObject = new { id = "u1", status = "created" };
            var payload = JsonSerializer.Serialize(payloadObject);
            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var cancellationToken = CancellationToken.None;

            _connectionMock.Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), cancellationToken)).ReturnsAsync(response);

            var result = await _service.CreateUserAsync(request, cancellationToken);

            Assert.NotNull(result);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), cancellationToken), Times.Once());
            _connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUserAsync_Throws_WhenConnectionThrows()
        {
            var request = new CreateUserRequest();
            var cancellationToken = CancellationToken.None;
            var exception = new HttpRequestException("network error");

            _connectionMock.Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), cancellationToken)).ThrowsAsync(exception);

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => _service.CreateUserAsync(request, cancellationToken));

            Assert.Equal("network error", ex.Message);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), cancellationToken), Times.Once());
            _connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsValue_WhenApiSucceeds()
        {
            var request = new UpdateUserRequest();
            var payloadObject = new { id = "u1", status = "updated" };
            var payload = JsonSerializer.Serialize(payloadObject);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var cancellationToken = CancellationToken.None;

            _connectionMock.Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u1", It.IsAny<string>(), cancellationToken)).ReturnsAsync(response);

            var result = await _service.UpdateUserAsync("u1", request, cancellationToken);

            Assert.NotNull(result);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u1", It.IsAny<string>(), cancellationToken), Times.Once());
            _connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateUserAsync_Throws_WhenConnectionReturnsFailure()
        {
            var request = new UpdateUserRequest();
            var cancellationToken = CancellationToken.None;
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing", Encoding.UTF8, "text/plain")
            };

            _connectionMock.Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u1", It.IsAny<string>(), cancellationToken)).ReturnsAsync(response);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateUserAsync("u1", request, cancellationToken));

            Assert.Contains("Zoho API error", ex.Message);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u1", It.IsAny<string>(), cancellationToken), Times.Once());
            _connectionMock.VerifyNoOtherCalls();
        }
    }
}
