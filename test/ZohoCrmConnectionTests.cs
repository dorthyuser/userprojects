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
        public void Constructor_ThrowsInvalidOperationException_WhenOptionsAreMissing()
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<ZohoCrmConnection>>();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new ZohoCrmConnection(mockFactory.Object, null!, mockLogger.Object));

            Assert.Equal("ZohoOptions not provided", ex.Message);
        }

        [Fact]
        public async Task SendAsync_RefreshesTokenAndReturnsApiResponse_WhenTokenIsExpired()
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions
            {
                ClientId = "client",
                ClientSecret = "secret",
                RefreshToken = "refresh",
                TokenUrl = "https://example.com/token",
                BaseUrl = "https://example.com"
            };

            var tokenHandler = new Mock<HttpMessageHandler>();
            var apiHandler = new Mock<HttpMessageHandler>();
            var tokenResponse = JsonSerializer.Serialize(new
            {
                access_token = "token-123",
                api_domain = "https://api.example.com/",
                expires_in = 3600
            });
            var apiResponseBody = JsonSerializer.Serialize(new { data = "ok" });

            tokenHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(tokenResponse, Encoding.UTF8, "application/json")
                });

            apiHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(apiResponseBody, Encoding.UTF8, "application/json")
                });

            var tokenClient = new HttpClient(tokenHandler.Object)
            {
                BaseAddress = new Uri("https://example.com")
            };
            var apiClient = new HttpClient(apiHandler.Object)
            {
                BaseAddress = new Uri("https://example.com")
            };

            mockFactory.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            mockFactory.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            var sut = new ZohoCrmConnection(mockFactory.Object, options, mockLogger.Object);

            var result = await sut.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None);
            var body = await result.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Contains("ok", body);
            Assert.Equal("https://api.example.com", options.BaseUrl);
            mockFactory.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            mockFactory.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }

        [Fact]
        public async Task SendAsync_ThrowsInvalidOperationException_WhenTokenRefreshFailsWithNonSuccessStatus()
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions
            {
                ClientId = "client",
                ClientSecret = "secret",
                RefreshToken = "refresh",
                TokenUrl = "https://example.com/token",
                BaseUrl = "https://example.com"
            };

            var tokenHandler = new Mock<HttpMessageHandler>();
            var apiHandler = new Mock<HttpMessageHandler>();

            tokenHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("bad token", Encoding.UTF8, "application/json")
                });

            var tokenClient = new HttpClient(tokenHandler.Object)
            {
                BaseAddress = new Uri("https://example.com")
            };
            var apiClient = new HttpClient(apiHandler.Object)
            {
                BaseAddress = new Uri("https://example.com")
            };

            mockFactory.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            mockFactory.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            var sut = new ZohoCrmConnection(mockFactory.Object, options, mockLogger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None));

            Assert.Contains("Token refresh failed", ex.Message);
        }

        [Fact]
        public async Task SendAsync_ThrowsInvalidOperationException_WhenTokenRefreshReturnsInvalidJson()
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions
            {
                ClientId = "client",
                ClientSecret = "secret",
                RefreshToken = "refresh",
                TokenUrl = "https://example.com/token",
                BaseUrl = "https://example.com"
            };

            var tokenHandler = new Mock<HttpMessageHandler>();
            var apiHandler = new Mock<HttpMessageHandler>();

            tokenHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("not-json", Encoding.UTF8, "application/json")
                });

            var tokenClient = new HttpClient(tokenHandler.Object)
            {
                BaseAddress = new Uri("https://example.com")
            };
            var apiClient = new HttpClient(apiHandler.Object)
            {
                BaseAddress = new Uri("https://example.com")
            };

            mockFactory.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            mockFactory.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            var sut = new ZohoCrmConnection(mockFactory.Object, options, mockLogger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None));

            Assert.Contains("invalid json", ex.Message);
        }

        [Fact]
        public async Task SendAsync_ThrowsInvalidOperationException_WhenTokenRefreshThrowsUnexpectedException()
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            var mockLogger = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions
            {
                ClientId = "client",
                ClientSecret = "secret",
                RefreshToken = "refresh",
                TokenUrl = "https://example.com/token",
                BaseUrl = "https://example.com"
            };

            var tokenHandler = new Mock<HttpMessageHandler>();
            var apiHandler = new Mock<HttpMessageHandler>();

            tokenHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("network down"));

            var tokenClient = new HttpClient(tokenHandler.Object)
            {
                BaseAddress = new Uri("https://example.com")
            };
            var apiClient = new HttpClient(apiHandler.Object)
            {
                BaseAddress = new Uri("https://example.com")
            };

            mockFactory.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            mockFactory.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            var sut = new ZohoCrmConnection(mockFactory.Object, options, mockLogger.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None));

            Assert.Contains("Token refresh failed", ex.Message);
            Assert.NotNull(ex.InnerException);
        }
    }
}
