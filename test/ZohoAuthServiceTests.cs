using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using hello_http_test.Services;
using hello_http_test.Models;

namespace hello_http_test.Tests
{
    public class ZohoAuthServiceTests
    {
        private readonly NullLogger<ZohoAuthService> _logger = new();

        [Fact]
        public async Task RefreshTokenAsync_Throws_WhenNoRefreshConfigured()
        {
            var factory = new TestHttpClientFactory();
            var cfg = TestConfig.Build(new Dictionary<string, string>());
            var svc = new ZohoAuthService(factory, cfg, _logger);

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RefreshTokenAsync());
        }

        [Fact]
        public async Task GetAccessTokenAsync_FetchesClientCredentialsToken()
        {
            var payload = JsonSerializer.Serialize(new TokenResponse { AccessToken = "tok", ExpiresIn = 3600 });
            var handler = new FakeHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });

            var client = new HttpClient(handler);
            var factory = new TestHttpClientFactory();
            factory.AddClient("default", client); // ZohoAuthService.CreateClient() uses no name

            var values = new Dictionary<string, string>
            {
                { "zoho_Auth_Token_url", "https://token" },
                { "zoho_client_id", "cid" },
                { "zoho_client_secret", "secret" },
                { "TOKEN_SCOPE", "scope" }
            };
            var cfg = TestConfig.Build(values);
            var svc = new ZohoAuthService(factory, cfg, _logger);

            var token = await svc.GetAccessTokenAsync();
            Assert.Equal("tok", token);
        }
    }
}
