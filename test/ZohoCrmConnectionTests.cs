using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using zoho_project_csharp.Services;
using zoho_project_csharp.Models;

namespace zoho_project_csharp.Tests
{
    // Minimal test logger to satisfy ILogger dependency
    internal class FakeLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    // Simple handler that uses provided delegate
    internal class DelegateHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _func;
        public DelegateHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> func) => _func = func;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _func(request, cancellationToken);
    }

    // Simple IHttpClientFactory which returns pre-built clients by name
    internal class SimpleHttpClientFactory : IHttpClientFactory
    {
        private readonly ConcurrentDictionary<string, HttpClient> _map = new();
        public void Register(string name, HttpClient client) => _map[name] = client;
        public HttpClient CreateClient(string name) => _map.TryGetValue(name, out var c) ? c : new HttpClient();
    }

    public class ZohoCrmConnectionTests
    {
        [Fact]
        public async Task SendAsync_UnauthorizedThenRefresh_RetriesAndSucceeds()
        {
            // Arrange
            var tokenCallCount = 0;
            var tokenUrl = "https://token.example.com/oauth/v2/token";

            // Token handler returns different access_token on subsequent calls
            var tokenHandler = new DelegateHandler(async (req, ct) =>
            {
                tokenCallCount++;
                var token = tokenCallCount == 1 ? "token1" : "token2";
                var json = $"{"access_token":"{token}","expires_in":3600,"api_domain":"https://api.example.com"}";
                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return await Task.FromResult(resp);
            });

            var apiRequestCount = 0;
            // API handler: first call with token1 -> 401, second call with token2 -> 200
            var apiHandler = new DelegateHandler(async (req, ct) =>
            {
                apiRequestCount++;
                var auth = req.Headers.Authorization?.Parameter ?? string.Empty;
                if (auth == "token1")
                {
                    return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("unauth") });
                }
                if (auth == "token2")
                {
                    return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{"ok":true}", Encoding.UTF8, "application/json") });
                }
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad") });
            });

            var tokenClient = new HttpClient(tokenHandler) { BaseAddress = new Uri(tokenUrl) };
            var apiClient = new HttpClient(apiHandler) { BaseAddress = new Uri("https://should-be-overridden.example.com/") };

            var factory = new SimpleHttpClientFactory();
            factory.Register("zoho-token", tokenClient);
            factory.Register("zoho-api", apiClient);

            var options = new ZohoOptions
            {
                ClientId = "cid",
                ClientSecret = "csecret",
                RefreshToken = "rtoken",
                TokenUrl = tokenUrl,
                BaseUrl = "https://fallback.example.com"
            };

            var connection = new ZohoCrmConnection(factory, options, new FakeLogger<ZohoCrmConnection>());

            // Act
            var response = await connection.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{"ok":true}", content);
            Assert.True(tokenCallCount >= 2, "Expected token refresh to be called at least twice (initial + retry)");
            Assert.Equal(2, apiRequestCount);
        }
    }
}
