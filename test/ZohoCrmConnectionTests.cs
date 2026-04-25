// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZohoProject2.Models;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class ZohoCrmConnectionTests
    {
        [Fact]
        public void Ctor_ThrowsInvalidOperationException_WhenOptionsAreNull()
        {
            // Arrange
            var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>(MockBehavior.Loose);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(factoryMock.Object, null!, loggerMock.Object));
            Assert.Equal("ZohoOptions not provided", exception.Message);
        }

        [Fact]
        public void Ctor_CreatesClients_WhenOptionsAreProvided()
        {
            // Arrange
            var apiClient = new HttpClient(new HttpMessageHandlerStub(new HttpResponseMessage(HttpStatusCode.OK)));
            var tokenClient = new HttpClient(new HttpMessageHandlerStub(new HttpResponseMessage(HttpStatusCode.OK)));
            var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>(MockBehavior.Loose);
            var options = new ZohoOptions();
            factoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            // Act
            var connection = new ZohoCrmConnection(factoryMock.Object, options, loggerMock.Object);

            // Assert
            Assert.NotNull(connection);
            factoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            factoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }

        private sealed class HttpMessageHandlerStub : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public HttpMessageHandlerStub(HttpResponseMessage response)
            {
                _response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }
    }
}