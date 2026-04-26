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
        [Fact]
        public async Task GetUsersAsync_ReturnsParsedResult_OnSuccess()
        {
            var json = "{'users':[{'id':'1','name':'Alice'}]}".Replace('\'', '"');
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await service.GetUsersAsync(CancellationToken.None);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_OnApiError()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request", Encoding.UTF8, "text/plain")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUsersAsync(CancellationToken.None));

            Assert.Contains("Zoho API error", exception.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ReturnsParsedResult_OnSuccess()
        {
            var request = new CreateUserRequest();
            var json = "{'id':'123','status':'created'}".Replace('\'', '"');
            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await service.CreateUserAsync(request, CancellationToken.None);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsInvalidOperationException_OnApiError()
        {
            var request = new CreateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("server error", Encoding.UTF8, "text/plain")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(request, CancellationToken.None));

            Assert.Contains("Zoho API error", exception.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Post, "/crm/v2/users", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ReturnsParsedResult_OnSuccess()
        {
            var request = new UpdateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{'id':'5','status':'updated'}".Replace('\'', '"'), Encoding.UTF8, "application/json")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/5", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var result = await service.UpdateUserAsync("5", request, CancellationToken.None);

            Assert.NotNull(result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/5", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsInvalidOperationException_OnApiError()
        {
            var request = new UpdateUserRequest();
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("not found", Encoding.UTF8, "text/plain")
            };
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/5", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var loggerMock = new Mock<ILogger<ZohoCrmService>>();
            var service = new ZohoCrmService(connectionMock.Object, loggerMock.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUserAsync("5", request, CancellationToken.None));

            Assert.Contains("Zoho API error", exception.Message);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Put, "/crm/v2/users/5", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}