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
            // Arrange
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var cancellationToken = new CancellationTokenSource().Token;
            var responsePayload = new[] { new { id = "u1", name = "Alice" } };
            var json = JsonSerializer.Serialize(new { data = responsePayload });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.GetUsersAsync(cancellationToken);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenConnectionFails()
        {
            // Arrange
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var cancellationToken = new CancellationTokenSource().Token;
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request", Encoding.UTF8, "text/plain")
            };

            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(response);

            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync(cancellationToken));
            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }
    }
}