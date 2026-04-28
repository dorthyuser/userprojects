// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
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
        public void Constructor_Throws_WhenOptionsAreNull()
        {
            var factoryMock = new Mock<IHttpClientFactory>();
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new ZohoCrmConnection(factoryMock.Object, null!, loggerMock.Object));

            Assert.Equal("ZohoOptions not provided", ex.Message);
        }

        [Fact]
        public async Task SendAsync_Throws_WhenTokenRefreshFails()
        {
            var tokenHandler = new Mock<HttpMessageHandler>();
            tokenHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("failed")
                });

            var apiHandler = new Mock<HttpMessageHandler>();
            apiHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });

            var apiClient = new HttpClient(new DelegatingHandlerStub(apiHandler.Object))
            {
                BaseAddress = new Uri("https://api.example.com")
            };
            var tokenClient = new HttpClient(new DelegatingHandlerStub(tokenHandler.Object))
            {
                BaseAddress = new Uri("https://token.example.com")
            };

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            var options = new ZohoOptions
            {
                ClientId = "client",
                ClientSecret = "secret",
                RefreshToken = "refresh",
                TokenUrl = "https://token.example.com/token",
                BaseUrl = "https://api.example.com"
            };

            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            var sut = new ZohoCrmConnection(factoryMock.Object, options, loggerMock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None));

            Assert.Contains("Token refresh failed", ex.Message);
        }

        private sealed class DelegatingHandlerStub : DelegatingHandler
        {
            public DelegatingHandlerStub(HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
            }
        }
    }
}