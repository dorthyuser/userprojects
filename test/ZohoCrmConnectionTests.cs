// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using ZohoProject2.Models;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class ZohoCrmConnectionTests
    {
        [Fact]
        public async Task SendAsync_ReturnsResponse_WhenHttpClientRespondsSuccessfully()
        {
            var tokenResponse = JsonSerializer.Serialize(new
            {
                access_token = "access-token",
                expires_in = 3600
            });
            var apiResponse = JsonSerializer.Serialize(new { data = new[] { new { id = "1" } } });

            var tokenHandler = new Mock<HttpMessageHandler>();
            tokenHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.AbsoluteUri == "https://accounts.example.com/oauth/v2/token"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(tokenResponse, Encoding.UTF8, "application/json")
                });

            var apiHandler = new Mock<HttpMessageHandler>();
            apiHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(apiResponse, Encoding.UTF8, "application/json")
                });

            var options = new ZohoOptions
            {
                ClientId = "client",
                ClientSecret = "secret",
                RefreshToken = "refresh",
                TokenUrl = "https://accounts.example.com/oauth/v2/token",
                BaseUrl = "https://api.example.com"
            };
            var logger = new Mock<ILogger<ZohoCrmConnection>>();

            var apiClient = new HttpClient(apiHandler.Object)
            {
                BaseAddress = new Uri(options.BaseUrl)
            };
            var tokenClient = new HttpClient(tokenHandler.Object)
            {
                BaseAddress = new Uri("https://accounts.example.com")
            };
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factory.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            var sut = new ZohoCrmConnection(factory.Object, options, logger.Object);

            var response = await sut.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            factory.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            factory.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }

        [Fact]
        public async Task SendAsync_ReturnsUnauthorized_WhenTokenRequestFails()
        {
            var tokenHandler = new Mock<HttpMessageHandler>();
            tokenHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.AbsoluteUri == "https://accounts.example.com/oauth/v2/token"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("unauthorized")
                });

            var apiHandler = new Mock<HttpMessageHandler>();
            apiHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("unauthorized")
                });

            var options = new ZohoOptions
            {
                ClientId = "client",
                ClientSecret = "secret",
                RefreshToken = "refresh",
                TokenUrl = "https://accounts.example.com/oauth/v2/token",
                BaseUrl = "https://api.example.com"
            };
            var logger = new Mock<ILogger<ZohoCrmConnection>>();

            var apiClient = new HttpClient(apiHandler.Object)
            {
                BaseAddress = new Uri(options.BaseUrl)
            };
            var tokenClient = new HttpClient(tokenHandler.Object)
            {
                BaseAddress = new Uri("https://accounts.example.com")
            };
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factory.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            var sut = new ZohoCrmConnection(factory.Object, options, logger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None));

            Assert.Contains("Token refresh failed", ex.Message);
            factory.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            factory.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }

        [Fact]
        public void Constructor_Throws_WhenOptionsAreNull()
        {
            var factory = new Mock<IHttpClientFactory>();
            var logger = new Mock<ILogger<ZohoCrmConnection>>();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new ZohoCrmConnection(factory.Object, null!, logger.Object));

            Assert.Equal("ZohoOptions not provided", ex.Message);
        }
    }
}