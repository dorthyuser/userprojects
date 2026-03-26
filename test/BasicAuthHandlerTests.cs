using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ResponseHttp.Services;
using Xunit;

namespace ResponseHttp.Tests
{
    public class BasicAuthHandlerTests
    {
        private class CaptureHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }
            private readonly HttpResponseMessage _response;
            public CaptureHandler(HttpResponseMessage response) => _response = response;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(_response);
            }
        }

        [Fact]
        public async Task SendAsync_AppendsBasicAuthHeader_WhenCredentialsPresent()
        {
            // Arrange
            var inMemory = new Dictionary<string, string>
            {
                ["$testuser"] = "user1",
                ["$testpass"] = "p@ss"
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
            var capture = new CaptureHandler(response);

            var handler = new BasicAuthHandler(configuration, LoggerFactory.Create(b => { }).CreateLogger<BasicAuthHandler>())
            {
                InnerHandler = capture
            };

            var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.local/");

            // Act
            var resp = await invoker.SendAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.NotNull(capture.LastRequest);
            Assert.True(capture.LastRequest.Headers.Authorization != null, "Authorization header should be present");
            Assert.Equal("Basic", capture.LastRequest.Headers.Authorization.Scheme);
            var encoded = capture.LastRequest.Headers.Authorization.Parameter;
            Assert.False(string.IsNullOrEmpty(encoded));
            // verify basic decode roundtrip
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            Assert.Equal("user1:p@ss", decoded);
        }

        [Fact]
        public async Task SendAsync_Throws_WhenCredentialsMissing()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
            var capture = new CaptureHandler(response);

            var handler = new BasicAuthHandler(configuration, LoggerFactory.Create(b => { }).CreateLogger<BasicAuthHandler>())
            {
                InnerHandler = capture
            };

            var invoker = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.local/");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.SendAsync(request, CancellationToken.None));
        }
    }
}
