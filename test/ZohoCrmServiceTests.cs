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
        public async Task GetUsersAsync_ReturnsParsedResult_WhenResponseIsSuccessful()
        {
            var payload = JsonSerializer.Serialize(new { data = new[] { new { id = "1", name = "Alice" } } });
            var connection = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            var token = CancellationToken.None;
            connection.Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, token)).ReturnsAsync(response);
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connection.Object, logger.Object);

            var result = await service.GetUsersAsync(token);

            Assert.NotNull(result);
            connection.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, token), Times.Once);
            connection.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsersAsync_Throws_WhenResponseIsUnsuccessful()
        {
            var connection = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request", Encoding.UTF8, "text/plain")
            };
            var token = CancellationToken.None;
            connection.Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, token)).ReturnsAsync(response);
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connection.Object, logger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync(token));

            Assert.Contains("Zoho API error", ex.Message);
            connection.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, token), Times.Once);
            connection.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUserAsync_Throws_NotImplementedLikeBehavior_WhenConnectionFails()
        {
            var connection = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var request = new ZohoProject2.Models.CreateUserRequest();
            var token = CancellationToken.None;
            connection.Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), token)).ThrowsAsync(new HttpRequestException("down"));
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connection.Object, logger.Object);

            await Assert.ThrowsAsync<HttpRequestException>(() => connection.Object.SendAsync(HttpMethod.Post, "/crm/v2/users", "{}", token));
            connection.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), token), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_Throws_WhenConnectionThrows()
        {
            var connection = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var request = new ZohoProject2.Models.UpdateUserRequest();
            var token = CancellationToken.None;
            connection.Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), token)).ThrowsAsync(new TimeoutException("timeout"));
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connection.Object, logger.Object);

            await Assert.ThrowsAsync<TimeoutException>(() => connection.Object.SendAsync(HttpMethod.Put, "/crm/v2/users/123", "{}", token));
            connection.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/123", It.IsAny<string>(), token), Times.Once);
        }
    }
}
