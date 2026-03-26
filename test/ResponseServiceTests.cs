using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ResponseHttp.Services;
using Xunit;

namespace ResponseHttp.Tests
{
    public class ResponseServiceTests
    {
        private class TestHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;
            public TestHttpClientFactory(HttpClient client) => _client = client;
            public HttpClient CreateClient(string name) => _client;
        }

        private class SimpleHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;
            public SimpleHandler(HttpResponseMessage response) => _response = response;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
                => Task.FromResult(_response);
        }

        [Fact]
        public async Task FetchAndFormatResponseAsync_ReturnsConcatenatedString_OnSuccess()
        {
            // Arrange
            var content = "hello";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };

            var handler = new SimpleHandler(httpResponse);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://example.test")
            };

            var factory = new TestHttpClientFactory(client);
            var configuration = new ConfigurationBuilder().Build(); // no RequestPath
            var logger = NullLogger<ResponseService>.Instance;

            var svc = new ResponseService(factory, logger, configuration);

            // Act
            var result = await svc.FetchAndFormatResponseAsync();

            // Assert
            Assert.Equal($"Response received- <<{content}>>.", result);
        }

        [Fact]
        public async Task FetchAndFormatResponseAsync_Throws_WhenNoEndpointConfigured()
        {
            // Arrange
            var client = new HttpClient(); // BaseAddress null
            var factory = new TestHttpClientFactory(client);
            var configuration = new ConfigurationBuilder().Build();
            var logger = NullLogger<ResponseService>.Instance;

            var svc = new ResponseService(factory, logger, configuration);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.FetchAndFormatResponseAsync());
        }
    }
}
