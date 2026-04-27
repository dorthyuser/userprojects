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
            var payload = JsonSerializer.Serialize(new { data = new[] { new { id = "1", name = "Alice" } } });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await sut.GetUsersAsync(CancellationToken.None);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(
                It.IsAny<HttpMethod>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenConnectionReturnsFailure()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("error", Encoding.UTF8, "text/plain")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await sut.GetUsersAsync(CancellationToken.None));

            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(
                It.IsAny<HttpMethod>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenConnectionReturnsInvalidJson()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json", Encoding.UTF8, "text/plain")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await sut.GetUsersAsync(CancellationToken.None));

            Assert.Contains("Failed to parse Zoho response", ex.Message);
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsResult_WhenConnectionSucceeds()
        {
            var request = new CreateUserRequest();
            var payload = JsonSerializer.Serialize(new { data = Array.Empty<object>() });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await sut.CreateUserAsync(request, CancellationToken.None);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(
                It.IsAny<HttpMethod>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsException_WhenRequestIsNull()
        {
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await sut.CreateUserAsync(null!, CancellationToken.None));
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsInvalidOperationException_WhenConnectionReturnsFailure()
        {
            var request = new CreateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("fail", Encoding.UTF8, "text/plain")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await sut.CreateUserAsync(request, CancellationToken.None));

            Assert.Contains("Zoho API error", ex.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsResult_WhenConnectionSucceeds()
        {
            var request = new UpdateUserRequest();
            var payload = JsonSerializer.Serialize(new { data = Array.Empty<object>() });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await sut.UpdateUserAsync("123", request, CancellationToken.None);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(
                It.IsAny<HttpMethod>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsException_WhenIdIsNullOrEmpty()
        {
            var request = new UpdateUserRequest();
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await sut.UpdateUserAsync(string.Empty, request, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsInvalidOperationException_WhenConnectionReturnsFailure()
        {
            var request = new UpdateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing", Encoding.UTF8, "text/plain")
            };

            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await sut.UpdateUserAsync("123", request, CancellationToken.None));

            Assert.Contains("Zoho API error", ex.Message);
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsInvalidOperationException_WhenConnectionThrows()
        {
            var request = new CreateUserRequest();
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("network"));

            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            await Assert.ThrowsAsync<HttpRequestException>(async () =>
                await sut.CreateUserAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsInvalidOperationException_WhenConnectionThrows()
        {
            var request = new UpdateUserRequest();
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(
                    It.IsAny<HttpMethod>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("timeout"));

            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            await Assert.ThrowsAsync<TimeoutException>(async () =>
                await sut.UpdateUserAsync("123", request, CancellationToken.None));
        }
    }
}
