using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using hello_http_test.Services;

namespace hello_http_test.Tests
{
    public class TokenServiceTests
    {
        private readonly NullLogger<TokenService> _logger = new();

        [Fact]
        public async Task GetAccessTokenAsync_AcquiresToken_FromTokenEndpoint()
        {
            var payload = JsonSerializer.Serialize(new { access_token = "abc", expires_in = 3600 });
            var handler = new FakeHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });

            var httpClient = new HttpClient(handler);
            var factory = new TestHttpClientFactory();
            factory.AddClient("zoho-token-client", httpClient);

            Environment.SetEnvironmentVariable("zoho_Auth_Token_url", "https://token");
            var cfg = TestConfig.Build(null);
            var svc = new TokenService(factory, cfg, _logger);

            var token = await svc.GetAccessTokenAsync();
            Assert.Equal("abc", token);

            // cleanup
            Environment.SetEnvironmentVariable("zoho_Auth_Token_url", null);
        }

        [Fact]
        public async Task ForceRefreshAsync_UpdatesToken()
        {
            var payload = JsonSerializer.Serialize(new { access_token = "refreshed", expires_in = 3600 });
            var handler = new FakeHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });

            var httpClient = new HttpClient(handler);
            var factory = new TestHttpClientFactory();
            factory.AddClient("zoho-token-client", httpClient);

            Environment.SetEnvironmentVariable("zoho_Auth_Token_url", "https://token");
            Environment.SetEnvironmentVariable("Zoho_ref_token", "rt");
            Environment.SetEnvironmentVariable("zoho_ref_GRANT_TYPE", "refresh_token");
            Environment.SetEnvironmentVariable("zoho_ref_client_id", "cid");
            Environment.SetEnvironmentVariable("zoho_ref_client_secret", "secret");

            var cfg = TestConfig.Build(null);
            var svc = new TokenService(factory, cfg, _logger);

            await svc.ForceRefreshAsync();
            var token = await svc.GetAccessTokenAsync();
            Assert.Equal("refreshed", token);

            // cleanup
            Environment.SetEnvironmentVariable("zoho_Auth_Token_url", null);
            Environment.SetEnvironmentVariable("Zoho_ref_token", null);
            Environment.SetEnvironmentVariable("zoho_ref_GRANT_TYPE", null);
            Environment.SetEnvironmentVariable("zoho_ref_client_id", null);
            Environment.SetEnvironmentVariable("zoho_ref_client_secret", null);
        }
    }
}
