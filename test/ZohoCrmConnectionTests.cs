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
        public void Constructor_Throws_WhenOptionsAreNull()
        {
            var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();

            var ex = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(factoryMock.Object, null!, loggerMock.Object));

            Assert.Equal("ZohoOptions not provided", ex.Message);
        }

        [Fact]
        public void Constructor_CreatesClients_WhenOptionsAreProvided()
        {
            var apiClient = new HttpClient(new FakeHandler(HttpStatusCode.OK));
            var tokenClient = new HttpClient(new FakeHandler(HttpStatusCode.OK));
            var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            factoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);
            var options = new ZohoOptions();
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();

            var connection = new ZohoCrmConnection(factoryMock.Object, options, loggerMock.Object);

            Assert.NotNull(connection);
            factoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once);
            factoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once);
        }

        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;

            public FakeHandler(HttpStatusCode statusCode)
            {
                _statusCode = statusCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent("{}")
                };
                return Task.FromResult(response);
            }
        }
    }
}