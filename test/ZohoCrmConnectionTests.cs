// GENERATED_BY_AI_TEST_ENGINE
using System;
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
        public void Constructor_Throws_WhenOptionsNull()
        {
            var factory = new Mock<IHttpClientFactory>();
            var logger = new Mock<ILogger<ZohoCrmConnection>>();

            var ex = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(factory.Object, null!, logger.Object));

            Assert.Equal("ZohoOptions not provided", ex.Message);
        }

        [Fact]
        public void Constructor_CreatesClients_WhenOptionsProvided()
        {
            var apiClient = new HttpClient(new StubHandler(new HttpResponseMessage(System.Net.HttpStatusCode.OK))) { BaseAddress = new Uri("https://example.com") };
            var tokenClient = new HttpClient(new StubHandler(new HttpResponseMessage(System.Net.HttpStatusCode.OK))) { BaseAddress = new Uri("https://example.com") };
            var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            factory.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factory.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);
            var logger = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions();

            var connection = new ZohoCrmConnection(factory.Object, options, logger.Object);

            Assert.NotNull(connection);
            factory.Verify(f => f.CreateClient("zoho_api"), Times.Once);
            factory.Verify(f => f.CreateClient("zoho_token"), Times.Once);
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;
            public StubHandler(HttpResponseMessage response) => _response = response;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(_response);
        }
    }
}
