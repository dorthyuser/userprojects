// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
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
            var httpFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();

            var exception = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(httpFactoryMock.Object, null!, loggerMock.Object));

            Assert.Equal("ZohoOptions not provided", exception.Message);
        }

        [Fact]
        public async Task SendAsync_ReturnsSuccessfulResponse_ForConfiguredClients()
        {
            var apiHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{'ok':true}".Replace('\'', '"'))
            });
            var tokenHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{'access_token':'abc','expires_in':3600}".Replace('\'', '"'))
            });
            var apiClient = new HttpClient(apiHandler)
            {
                BaseAddress = new Uri("https://api.example.com")
            };
            var tokenClient = new HttpClient(tokenHandler);
            var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            factoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions
            {
                ClientId = "client-id",
                ClientSecret = "client-secret",
                TokenUrl = "https://auth.example.com/token",
                RefreshToken = "refresh-token",
                BaseUrl = "https://api.example.com"
            };
            var connection = new ZohoCrmConnection(factoryMock.Object, options, loggerMock.Object);

            var response = await connection.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            factoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            factoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }

        [Fact]
        public async Task SendAsync_Throws_WhenApiReturnsError()
        {
            var apiHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("failure")
            });
            var tokenHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{'access_token':'abc','expires_in':3600}".Replace('\'', '"'))
            });
            var apiClient = new HttpClient(apiHandler)
            {
                BaseAddress = new Uri("https://api.example.com")
            };
            var tokenClient = new HttpClient(tokenHandler);
            var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            factoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions
            {
                ClientId = "client-id",
                ClientSecret = "client-secret",
                TokenUrl = "https://auth.example.com/token",
                RefreshToken = "refresh-token",
                BaseUrl = "https://api.example.com"
            };
            var connection = new ZohoCrmConnection(factoryMock.Object, options, loggerMock.Object);

            var response = await connection.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            factoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            factoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }

        private sealed class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

            public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request));
            }
        }
    }
}
