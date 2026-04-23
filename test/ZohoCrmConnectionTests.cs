using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using zoho_project_csharp.Models;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Tests
{
    public class ZohoCrmConnectionTests
    {
        private class QueueHandler : HttpMessageHandler
        {
            private readonly ConcurrentQueue<HttpResponseMessage> _responses = new ConcurrentQueue<HttpResponseMessage>();

            public void Enqueue(HttpResponseMessage msg) => _responses.Enqueue(msg);

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (_responses.TryDequeue(out var resp))
                {
                    // Return a clone to allow disposal by caller independently
                    var clone = new HttpResponseMessage(resp.StatusCode)
                    {
                        Content = resp.Content == null ? null : new StringContent(resp.Content.ReadAsStringAsync().GetAwaiter().GetResult(), Encoding.UTF8, resp.Content.Headers.ContentType?.MediaType ?? "text/plain")
                    };
                    return Task.FromResult(clone);
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        private class SimpleHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
            public SimpleHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_responder(request));
        }

        private class FakeFactory : IHttpClientFactory
        {
            private readonly IDictionary<string, HttpClient> _map;
            public FakeFactory(IDictionary<string, HttpClient> map) => _map = map;
            public HttpClient CreateClient(string name) => _map.ContainsKey(name) ? _map[name] : new HttpClient();
        }

        private class NoopLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
            private class NullScope : IDisposable { public static NullScope Instance { get; } = new NullScope(); public void Dispose() { } }
        }

        [Fact]
        public async Task SendAsync_PerformsTokenRefreshAndCallsApi()
        {
            // Arrange
            var tokenJson = "{"access_token":"token1","expires_in":3600,"api_domain":"https://api.example.com"}";
            var tokenResp = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(tokenJson, Encoding.UTF8, "application/json") };

            var apiResp = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("api-response", Encoding.UTF8, "text/plain") };

            var tokenHandler = new QueueHandler();
            tokenHandler.Enqueue(tokenResp);

            var apiHandler = new SimpleHandler(req =>
            {
                // Validate Authorization header present
                if (!req.Headers.Contains("Authorization"))
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                return apiResp;
            });

            var tokenClient = new HttpClient(tokenHandler) { BaseAddress = null };
            var apiClient = new HttpClient(apiHandler) { BaseAddress = new Uri("https://api.example.com") };

            var factory = new FakeFactory(new Dictionary<string, HttpClient>
            {
                ["zoho-token"] = tokenClient,
                ["zoho-api"] = apiClient
            });

            var options = new ZohoOptions
            {
                ClientId = "cid",
                ClientSecret = "cs",
                RefreshToken = "rt",
                TokenUrl = "https://token.example.com",
                BaseUrl = "https://base.example.com"
            };

            var conn = new ZohoCrmConnection(factory, options, new NoopLogger<ZohoCrmConnection>());

            // Act
            using var resp = await conn.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None);
            var content = await resp.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("api-response", content);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task SendAsync_OnUnauthorized_RefreshesAndRetries()
        {
            // Arrange
            var tokenResp1 = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{"access_token":"t1","expires_in":3600}", Encoding.UTF8, "application/json") };
            var tokenResp2 = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{"access_token":"t2","expires_in":3600}", Encoding.UTF8, "application/json") };

            var tokenHandler = new QueueHandler();
            tokenHandler.Enqueue(tokenResp1);
            tokenHandler.Enqueue(tokenResp2);

            var apiHandler = new QueueHandler();
            // First API call returns 401
            apiHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("unauth", Encoding.UTF8, "text/plain") });
            // Second API call returns success
            apiHandler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok-after-retry", Encoding.UTF8, "text/plain") });

            var tokenClient = new HttpClient(tokenHandler) { BaseAddress = null };
            var apiClient = new HttpClient(apiHandler) { BaseAddress = new Uri("https://base.example.com") };

            var factory = new FakeFactory(new Dictionary<string, HttpClient>
            {
                ["zoho-token"] = tokenClient,
                ["zoho-api"] = apiClient
            });

            var options = new ZohoOptions
            {
                ClientId = "cid",
                ClientSecret = "cs",
                RefreshToken = "rt",
                TokenUrl = "https://token.example.com",
                BaseUrl = "https://base.example.com"
            };

            var conn = new ZohoCrmConnection(factory, options, new NoopLogger<ZohoCrmConnection>());

            // Act
            using var resp = await conn.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None);
            var content = await resp.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("ok-after-retry", content);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }
}
