// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
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
        public async Task GetUsersAsync_ReturnsDeserializedObject_WhenResponseIsSuccessful()
        {
            var mockConnection = new Mock<IZohoCrmConnection>();
            var mockLogger = new Mock<ILogger<ZohoCrmService>>();
            var body = JsonSerializer.Serialize(new { users = new[] { new { id = "1", full_name = "Test User" } } });
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

            var sut = new ZohoCrmService(mockConnection.Object, mockLogger.Object);

            var result = await sut.GetUsersAsync(CancellationToken.None);

            Assert.NotNull(result);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Get,
                "/crm/v2/users",
                null,
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenResponseFails()
        {
            var mockConnection = new Mock<IZohoCrmConnection>();
            var mockLogger = new Mock<ILogger<ZohoCrmService>>();
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("error")
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Get,
                    "/crm/v2/users",
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, mockLogger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetUsersAsync(CancellationToken.None));

            Assert.Contains("Zoho API error", ex.Message);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Get,
                "/crm/v2/users",
                null,
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsDeserializedObject_WhenResponseIsSuccessful()
        {
            var mockConnection = new Mock<IZohoCrmConnection>();
            var mockLogger = new Mock<ILogger<ZohoCrmService>>();
            var request = new CreateUserRequest
            {
                FullName = "Jane Doe",
                Email = "jane@example.com"
            };
            var body = JsonSerializer.Serialize(new { users = new[] { new { status = "success" } } });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Post,
                    "/crm/v2/users",
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, mockLogger.Object);

            var result = await sut.CreateUserAsync(request, CancellationToken.None);

            Assert.NotNull(result);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Post,
                "/crm/v2/users",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsInvalidOperationException_WhenResponseFails()
        {
            var mockConnection = new Mock<IZohoCrmConnection>();
            var mockLogger = new Mock<ILogger<ZohoCrmService>>();
            var request = new CreateUserRequest
            {
                FullName = "Jane Doe",
                Email = "jane@example.com"
            };
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Post,
                    "/crm/v2/users",
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, mockLogger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateUserAsync(request, CancellationToken.None));

            Assert.Contains("Zoho API error", ex.Message);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Post,
                "/crm/v2/users",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsDeserializedObject_WhenResponseIsSuccessful()
        {
            var mockConnection = new Mock<IZohoCrmConnection>();
            var mockLogger = new Mock<ILogger<ZohoCrmService>>();
            var request = new UpdateUserRequest
            {
                FullName = "Jane Doe",
                Email = "jane@example.com"
            };
            var body = JsonSerializer.Serialize(new { users = new[] { new { status = "updated" } } });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Put,
                    "/crm/v2/users/123",
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, mockLogger.Object);

            var result = await sut.UpdateUserAsync("123", request, CancellationToken.None);

            Assert.NotNull(result);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Put,
                "/crm/v2/users/123",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsInvalidOperationException_WhenResponseFails()
        {
            var mockConnection = new Mock<IZohoCrmConnection>();
            var mockLogger = new Mock<ILogger<ZohoCrmService>>();
            var request = new UpdateUserRequest
            {
                FullName = "Jane Doe",
                Email = "jane@example.com"
            };
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing")
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Put,
                    "/crm/v2/users/123",
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, mockLogger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateUserAsync("123", request, CancellationToken.None));

            Assert.Contains("Zoho API error", ex.Message);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Put,
                "/crm/v2/users/123",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}