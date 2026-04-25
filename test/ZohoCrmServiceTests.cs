// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Text;
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
        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string body)
        {
            var response = new HttpResponseMessage(statusCode);
            response.Content = new StringContent(body, Encoding.UTF8, "application/json");
            return response;
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsParsedResult_WhenConnectionSucceeds()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var expectedBody = "{ 'data': [ { 'id': 'u1', 'name': 'Alice' } ] }".Replace("'", """);
            var response = CreateResponse(HttpStatusCode.OK, expectedBody);
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.GetUsersAsync(cancellationToken);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenConnectionReturnsFailure()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var errorBody = "{ 'error': 'bad request' }".Replace("'", """);
            var response = CreateResponse(HttpStatusCode.BadRequest, errorBody);
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync(cancellationToken));

            // Assert
            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsResult_WhenConnectionSucceeds()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var expectedBody = "{ 'data': [ { 'id': 'u2' } ] }".Replace("'", """);
            var response = CreateResponse(HttpStatusCode.Created, expectedBody);
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.CreateUserAsync(new CreateUserRequest(), cancellationToken);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsInvalidOperationException_WhenConnectionReturnsFailure()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var errorBody = "{ 'error': 'unprocessable' }".Replace("'", """);
            var response = CreateResponse(HttpStatusCode.UnprocessableEntity, errorBody);
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(new CreateUserRequest(), cancellationToken));

            // Assert
            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsResult_WhenConnectionSucceeds()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var response = CreateResponse(HttpStatusCode.OK, "{ 'data': [ { 'id': 'u3' } ] }".Replace("'", """));
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u3", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var result = await service.UpdateUserAsync("u3", new UpdateUserRequest(), cancellationToken);

            // Assert
            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u3", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsInvalidOperationException_WhenConnectionReturnsFailure()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var response = CreateResponse(HttpStatusCode.InternalServerError, "{ 'error': 'server' }".Replace("'", """));
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u4", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUserAsync("u4", new UpdateUserRequest(), cancellationToken));

            // Assert
            Assert.Contains("Zoho API error", ex.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/u4", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
            connectionMock.VerifyNoOtherCalls();
        }
    }
}
