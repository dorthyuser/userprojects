// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
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
        public void Constructor_Throws_WhenOptionsAreNull()
        {
            // Arrange
            var httpFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(httpFactoryMock.Object, null!, loggerMock.Object));
            Assert.Equal("ZohoOptions not provided", exception.Message);
        }

        [Fact]
        public void Constructor_CreatesClients_WhenOptionsProvided()
        {
            // Arrange
            var apiClient = new HttpClient(new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)));
            var tokenClient = new HttpClient(new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)));
            var httpFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions();
            httpFactoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            httpFactoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            // Act
            var connection = new ZohoCrmConnection(httpFactoryMock.Object, options, loggerMock.Object);

            // Assert
            Assert.NotNull(connection);
            httpFactoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once);
            httpFactoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once);
            httpFactoryMock.VerifyNoOtherCalls();
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public StubHandler(HttpResponseMessage response)
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
