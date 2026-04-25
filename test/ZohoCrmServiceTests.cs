// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Tests
{
    public class ZohoCrmServiceTests
    {
        [Fact]
        public void Constructor_CreatesInstance_WhenDependenciesProvided()
        {
            // Arrange
            var mockConnection = new Mock<IZohoCrmConnection>();
            var mockLogger = new Mock<ILogger<ZohoCrmService>>();

            // Act
            var svc = new ZohoCrmService(mockConnection.Object, mockLogger.Object);

            // Assert
            Assert.NotNull(svc);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsContent_FromConnection()
        {
            // Arrange
            var mockConnection = new Mock<IZohoCrmConnection>();
            var payload = new[] { new { id = "u1" } };
            var json = JsonSerializer.Serialize(payload);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
            mockConnection.Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(response);
            var mockLogger = new Mock<ILogger<ZohoCrmService>>();
            var svc = new ZohoCrmService(mockConnection.Object, mockLogger.Object);

            // Act
            var result = await svc.GetUsersAsync(CancellationToken.None);

            // Assert
            var status = result.StatusCode;
            var content = result.Content;
            Assert.Equal((int)HttpStatusCode.OK, status);
            Assert.Equal(json, content);
            mockConnection.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_PropagatesException_FromConnection()
        {
            // Arrange
            var mockConnection = new Mock<IZohoCrmConnection>();
            mockConnection.Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new Exception("down"));
            var mockLogger = new Mock<ILogger<ZohoCrmService>>();
            var svc = new ZohoCrmService(mockConnection.Object, mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => svc.GetUsersAsync(CancellationToken.None));
            mockConnection.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
