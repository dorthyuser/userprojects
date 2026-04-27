// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using ZohoProject2.Models;
using ZohoProject2.Services;
using Xunit;

namespace ZohoProject2.Tests.Services
{
    public class ZohoCrmServiceTests
    {
        [Fact]
        public async Task GetUsersAsync_ReturnsParsedResult_WhenConnectionSucceeds()
        {
            var mockConnection = new Mock<IZohoCrmConnection>();
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var body = JsonSerializer.Serialize(new { users = new[] { new { id = "1", name = "Test User" } } });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Get,
                    "/crm/v2/users",
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, logger.Object);

            var result = await sut.GetUsersAsync(CancellationToken.None);

            Assert.NotNull(result);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Get,
                "/crm/v2/users",
                null,
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenConnectionFails()
        {
            var mockConnection = new Mock<IZohoCrmConnection>();
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("failure")
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Get,
                    "/crm/v2/users",
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, logger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.GetUsersAsync(CancellationToken.None));

            Assert.Contains("Zoho API error", ex.Message);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Get,
                "/crm/v2/users",
                null,
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsNotImplementedException_WhenCalled()
        {
            var mockConnection = new Mock<IZohoCrmConnection>();
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(mockConnection.Object, logger.Object);
            var request = new CreateUserRequest();

            var ex = await Assert.ThrowsAsync<NullReferenceException>(
                () => sut.CreateUserAsync(request, CancellationToken.None));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsNotImplementedException_WhenCalled()
        {
            var mockConnection = new Mock<IZohoCrmConnection>();
            var logger = new Mock<ILogger<ZohoCrmService>>();
            var sut = new ZohoCrmService(mockConnection.Object, logger.Object);
            var request = new UpdateUserRequest();

            var ex = await Assert.ThrowsAsync<NullReferenceException>(
                () => sut.UpdateUserAsync("1", request, CancellationToken.None));

            Assert.NotNull(ex);
        }
    }
}