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
        public async Task GetUsersAsync_ReturnsParsedResponse_WhenConnectionSucceeds()
        {
            var responseJson = JsonSerializer.Serialize(new { users = new[] { new { id = "1", name = "Jane" } } });
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await service.GetUsersAsync(CancellationToken.None);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenConnectionReturnsError()
        {
            var errorBody = "failed";
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(errorBody, Encoding.UTF8, "application/json")
                });
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync(CancellationToken.None));

            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsValue_WhenConnectionSucceeds()
        {
            var request = new CreateUserRequest();
            var responseJson = JsonSerializer.Serialize(new { data = Array.Empty<object>() });
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await service.CreateUserAsync(request, CancellationToken.None);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_Throws_WhenConnectionReturnsError()
        {
            var request = new CreateUserRequest();
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("bad request", Encoding.UTF8, "application/json")
                });
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(request, CancellationToken.None));

            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsValue_WhenConnectionSucceeds()
        {
            var request = new UpdateUserRequest();
            var responseJson = JsonSerializer.Serialize(new { data = Array.Empty<object>() });
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await service.UpdateUserAsync("123", request, CancellationToken.None);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_Throws_WhenConnectionReturnsError()
        {
            var request = new UpdateUserRequest();
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("not found", Encoding.UTF8, "application/json")
                });
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUserAsync("123", request, CancellationToken.None));

            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}