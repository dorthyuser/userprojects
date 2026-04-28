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
        public async Task GetUsersAsync_ReturnsDeserializedObject_WhenConnectionSucceeds()
        {
            // Arrange
            var mockConnection = new Mock<IZohoCrmConnection>();
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var body = JsonSerializer.Serialize(new
            {
                users = new[]
                {
                    new { id = "1", name = "Test User" }
                }
            });

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Get,
                    "/crm/v2/users",
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, loggerMock.Object);

            // Act
            var result = await sut.GetUsersAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Get,
                "/crm/v2/users",
                null,
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_Throws_WhenConnectionReturnsError()
        {
            // Arrange
            var mockConnection = new Mock<IZohoCrmConnection>();
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Get,
                    "/crm/v2/users",
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, loggerMock.Object);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.GetUsersAsync(CancellationToken.None));

            // Assert
            Assert.Contains("Zoho API error", ex.Message);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Get,
                "/crm/v2/users",
                null,
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsDeserializedObject_WhenConnectionSucceeds()
        {
            // Arrange
            var mockConnection = new Mock<IZohoCrmConnection>();
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var request = new CreateUserRequest();
            var responseBody = JsonSerializer.Serialize(new { data = new[] { new { id = "1" } } });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Post,
                    "/crm/v2/users",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, loggerMock.Object);

            // Act
            var result = await sut.CreateUserAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Post,
                "/crm/v2/users",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_Throws_WhenConnectionReturnsError()
        {
            // Arrange
            var mockConnection = new Mock<IZohoCrmConnection>();
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var request = new CreateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("error")
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Post,
                    "/crm/v2/users",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, loggerMock.Object);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.CreateUserAsync(request, CancellationToken.None));

            // Assert
            Assert.Contains("Zoho API error", ex.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsDeserializedObject_WhenConnectionSucceeds()
        {
            // Arrange
            var mockConnection = new Mock<IZohoCrmConnection>();
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var request = new UpdateUserRequest();
            var responseBody = JsonSerializer.Serialize(new { data = new[] { new { id = "1" } } });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Put,
                    "/crm/v2/users/123",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, loggerMock.Object);

            // Act
            var result = await sut.UpdateUserAsync("123", request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            mockConnection.Verify(c => c.SendAsync(
                HttpMethod.Put,
                "/crm/v2/users/123",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_Throws_WhenConnectionReturnsError()
        {
            // Arrange
            var mockConnection = new Mock<IZohoCrmConnection>();
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var request = new UpdateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("not found")
            };

            mockConnection
                .Setup(c => c.SendAsync(
                    HttpMethod.Put,
                    "/crm/v2/users/123",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var sut = new ZohoCrmService(mockConnection.Object, loggerMock.Object);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.UpdateUserAsync("123", request, CancellationToken.None));

            // Assert
            Assert.Contains("Zoho API error", ex.Message);
        }
    }
}