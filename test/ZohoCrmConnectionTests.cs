using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Xunit;
using zoho_project_csharp.Services;
using zoho_project_csharp.Models;
using Microsoft.Extensions.Logging;

namespace zoho_project_csharp.Tests
{
    public class ZohoCrmConnectionTests
    {
        private class FakeClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _api;
            private readonly HttpClient _token;
            public FakeClientFactory(HttpMessageHandler apiHandler, HttpMessageHandler tokenHandler)
            {
                _api = new HttpClient(apiHandler, disposeHandler: false);
                _token = new HttpClient(tokenHandler, disposeHandler: false);
            }
            public HttpClient CreateClient(string name)
            {
                return name switch
                {
                    "zoho-api" => _api,
                    "zoho-token" => _token,
                    _ => new HttpClient()
                };
            }
        }

        private class NoOpLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
            private class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new NullScope();
                public void Dispose() { }
            }
        }

        [Fact]
        public async Task RefreshToken_SetsAccessToken_AndUsesApiDomain()
        {
            var tokenCalled = 0;
            var tokenHandler = new DelegatingHandlerStub((req, ct) =>
            {
                tokenCalled++;
                var json = "{"access_token":"token1","expires_in":3600,"api_domain":"https://api.example/"}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
            });

            HttpRequestMessage? capturedApiRequest = null;
            var apiHandler = new DelegatingHandlerStub((req, ct) =>
            {
                capturedApiRequest = req;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });
            });

            var options = new ZohoOptions { BaseUrl = "https://base.example", ClientId = "c", ClientSecret = "s", RefreshToken = "r", TokenUrl = "https://token" };
            var factory = new FakeClientFactory(apiHandler, tokenHandler);
            var conn = new ZohoCrmConnection(factory, options, new NoOpLogger<ZohoCrmConnection>());

            using var resp = await conn.SendAsync(HttpMethod.Get, "/test", null, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.NotNull(capturedApiRequest);
            Assert.True(capturedApiRequest.Headers.Contains("Authorization") || capturedApiRequest.Headers.Authorization != null);

            string authValue = capturedApiRequest.Headers.Authorization != null
                ? $"{capturedApiRequest.Headers.Authorization.Scheme} {capturedApiRequest.Headers.Authorization.Parameter}"
                : string.Join(",", capturedApiRequest.Headers.GetValues("Authorization"));

            Assert.Contains("token1", authValue);
            Assert.StartsWith("https://api.example", capturedApiRequest.RequestUri!.AbsoluteUri);
            Assert.Equal(1, tokenCalled);
        }

        [Fact]
        public async Task SendAsync_Unauthorized_RetriesAfterRefresh()
        {
            var tokenCalls = 0;
            var tokenHandler = new DelegatingHandlerStub((req, ct) =>
            {
                tokenCalls++;
                var token = tokenCalls == 1 ? "oldtoken" : "newtoken";
                var json = $"{"access_token":"{token}","expires_in":3600}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
            });

            var apiCalls = 0;
            var apiHandler = new DelegatingHandlerStub((req, ct) =>
            {
                apiCalls++;
                var auth = req.Headers.Authorization?.Parameter ?? string.Empty;
                if (auth == "oldtoken")
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok", Encoding.UTF8, "text/plain") });
            });

            var options = new ZohoOptions { BaseUrl = "https://base.example", ClientId = "c", ClientSecret = "s", RefreshToken = "r", TokenUrl = "https://token" };
            var factory = new FakeClientFactory(apiHandler, tokenHandler);
            var conn = new ZohoCrmConnection(factory, options, new NoOpLogger<ZohoCrmConnection>());

            using var resp = await conn.SendAsync(HttpMethod.Get, "/x", null, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal(2, tokenCalls);
            Assert.True(apiCalls >= 2);
        }

        [Fact]
        public async Task RefreshToken_InvalidJson_ThrowsInvalidOperationException()
        {
            var tokenHandler = new DelegatingHandlerStub((req, ct) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-a-json", Encoding.UTF8, "application/json") });
            });

            var apiHandler = new DelegatingHandlerStub((req, ct) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });

            var options = new ZohoOptions { BaseUrl = "https://base.example", ClientId = "c", ClientSecret = "s", RefreshToken = "r", TokenUrl = "https://token" };
            var factory = new FakeClientFactory(apiHandler, tokenHandler);
            var conn = new ZohoCrmConnection(factory, options, new NoOpLogger<ZohoCrmConnection>());

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                using var _ = await conn.SendAsync(HttpMethod.Get, "/x", null, CancellationToken.None);
            });
        }

        [Fact]
        public async Task ShortExpiry_TokenRefreshedOnEachCall()
        {
            var tokenCalls = 0;
            var tokenHandler = new DelegatingHandlerStub((req, ct) =>
            {
                tokenCalls++;
                var json = $"{"access_token":"t{tokenCalls}","expires_in":30}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
            });

            var apiHandler = new DelegatingHandlerStub((req, ct) =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });
            });

            var options = new ZohoOptions { BaseUrl = "https://base.example", ClientId = "c", ClientSecret = "s", RefreshToken = "r", TokenUrl = "https://token" };
            var factory = new FakeClientFactory(apiHandler, tokenHandler);
            var conn = new ZohoCrmConnection(factory, options, new NoOpLogger<ZohoCrmConnection>());

            using var r1 = await conn.SendAsync(HttpMethod.Get, "/a", null, CancellationToken.None);
            using var r2 = await conn.SendAsync(HttpMethod.Get, "/b", null, CancellationToken.None);

            Assert.Equal(2, tokenCalls);
        }

        private class DelegatingHandlerStub : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _func;
            public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> func) => _func = func;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _func(request, cancellationToken);
        }
    }
}
