// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
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
        public void Ctor_Throws_WhenOptionsIsNull()
        {
            var httpFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();

            var ex = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(httpFactoryMock.Object, null!, loggerMock.Object));

            Assert.Equal("ZohoOptions not provided", ex.Message);
        }

        [Fact]
        public void Ctor_CreatesClients_WhenOptionsProvided()
        {
            var httpFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions();
            var apiClient = new HttpClient(new HttpClientHandlerStub());
            var tokenClient = new HttpClient(new HttpClientHandlerStub());
            httpFactoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            httpFactoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            var connection = new ZohoCrmConnection(httpFactoryMock.Object, options, loggerMock.Object);

            Assert.NotNull(connection);
            httpFactoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            httpFactoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }

        [Fact]
        public async Task SendAsync_ReturnsResponse_FromApiClient()
        {
            var httpFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions { BaseUrl = "https://example.com", TokenUrl = "https://example.com/token", ClientId = "id", ClientSecret = "secret", RefreshToken = "refresh" };
            var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("success")
            });
            var apiClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://example.com")
            };
            var tokenJson = JsonSerializer.Serialize(new { access_token = "token", expires_in = 3600 });
            var tokenClient = new HttpClient(new TokenHandlerStub(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(tokenJson)
            }))
            {
                BaseAddress = new Uri("https://example.com")
            };
            httpFactoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            httpFactoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);
            var connection = new ZohoCrmConnection(httpFactoryMock.Object, options, loggerMock.Object);

            var response = await connection.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("success", content);
            httpFactoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            httpFactoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }

        [Fact]
        public async Task SendAsync_Throws_WhenApiClientFails()
        {
            var httpFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions { BaseUrl = "https://example.com", TokenUrl = "https://example.com/token", ClientId = "id", ClientSecret = "secret", RefreshToken = "refresh" };
            var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("error")
            });
            var apiClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://example.com")
            };
            var tokenJson = JsonSerializer.Serialize(new { access_token = "token", expires_in = 3600 });
            var tokenClient = new HttpClient(new TokenHandlerStub(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(tokenJson)
            }))
            {
                BaseAddress = new Uri("https://example.com")
            };
            httpFactoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            httpFactoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);
            var connection = new ZohoCrmConnection(httpFactoryMock.Object, options, loggerMock.Object);

            var response = await connection.SendAsync(HttpMethod.Post, "/crm/v2/users", "{}", CancellationToken.None);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("error", content);
            httpFactoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            httpFactoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public StubHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }

        private sealed class TokenHandlerStub : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public TokenHandlerStub(HttpResponseMessage response)
            {
                _response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }

        private sealed class HttpClientHandlerStub : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("ok")
                });
            }
        }
    }
}