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
        public async Task GetUsersAsync_ReturnsParsedResult_WhenResponseIsSuccessful()
        {
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var payload = new[] { new { id = "u1", name = "Alice" } };
            var json = JsonSerializer.Serialize(payload);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var cancellationToken = CancellationToken.None;
            connectionMock.Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken)).ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await service.GetUsersAsync(cancellationToken);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_Throws_WhenResponseIsNotSuccessful()
        {
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            };
            var cancellationToken = CancellationToken.None;
            connectionMock.Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken)).ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync(cancellationToken));

            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsParsedResult_WhenResponseIsSuccessful()
        {
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var request = new CreateUserRequest();
            var payload = new { id = "u2", status = "created" };
            var json = JsonSerializer.Serialize(payload);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var cancellationToken = CancellationToken.None;
            connectionMock.Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken)).ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await service.CreateUserAsync(request, cancellationToken);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_Throws_WhenResponseIsNotSuccessful()
        {
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var request = new CreateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("server error")
            };
            var cancellationToken = CancellationToken.None;
            connectionMock.Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken)).ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(request, cancellationToken));

            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string?>(), cancellationToken), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsParsedResult_WhenResponseIsSuccessful()
        {
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var request = new UpdateUserRequest();
            var payload = new { id = "u3", status = "updated" };
            var json = JsonSerializer.Serialize(payload);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var cancellationToken = CancellationToken.None;
            connectionMock.Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string?>(), cancellationToken)).ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await service.UpdateUserAsync("123", request, cancellationToken);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string?>(), cancellationToken), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_Throws_WhenResponseIsNotSuccessful()
        {
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var request = new UpdateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("not found")
            };
            var cancellationToken = CancellationToken.None;
            connectionMock.Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string?>(), cancellationToken)).ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUserAsync("123", request, cancellationToken));

            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string?>(), cancellationToken), Times.Once());
        }
    }
}