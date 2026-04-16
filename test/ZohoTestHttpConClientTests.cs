using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using hello_http_test.Services;
using Microsoft.Extensions.Logging;

namespace hello_http_test.Tests
{
    public class ZohoTestHttpConClientTests
    {
        private readonly NullLogger<ZohoTestHttpConClient> _logger = new();

        [Fact]
        public async Task GetStoresAsync_ReturnsBody_WhenSuccess()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[1,2,3]", Encoding.UTF8, "application/json")
            });
            var client = new HttpClient(handler) { BaseAddress = new System.Uri("https://example/") };
            var cfg = TestConfig.Build(null);
            var api = new ZohoTestHttpConClient(client, _logger, cfg);

            var result = await api.GetStoresAsync();
            Assert.Equal("[1,2,3]", result);
        }

        [Fact]
        public async Task GetStoresAsync_Throws_WhenNonSuccess()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("err")
            });
            var client = new HttpClient(handler) { BaseAddress = new System.Uri("https://example/") };
            var cfg = TestConfig.Build(null);
            var api = new ZohoTestHttpConClient(client, _logger, cfg);

            await Assert.ThrowsAsync<HttpRequestException>(() => api.GetStoresAsync());
        }

        [Fact]
        public async Task CreateStoreAsync_ReturnsContent_WhenSuccess()
        {
            var handler = new FakeHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("created")
            });
            var client = new HttpClient(handler) { BaseAddress = new System.Uri("https://example/") };
            var cfg = TestConfig.Build(null);
            var api = new ZohoTestHttpConClient(client, _logger, cfg);

            var res = await api.CreateStoreAsync(new { name = "s" });
            Assert.Equal("created", res);
        }

        [Fact]
        public async Task UpdateStoreAsync_Throws_OnEmptyId()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });
            var client = new HttpClient(handler) { BaseAddress = new System.Uri("https://example/") };
            var cfg = TestConfig.Build(null);
            var api = new ZohoTestHttpConClient(client, _logger, cfg);

            await Assert.ThrowsAsync<System.ArgumentNullException>(() => api.UpdateStoreAsync("", new { }));
        }
    }
}
