// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
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
        public void Constructor_Throws_WhenOptionsAreNull()
        {
            // Arrange
            var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var logger = new Mock<ILogger<ZohoCrmConnection>>();

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(httpFactory.Object, null!, logger.Object));

            // Assert
            Assert.Equal("ZohoOptions not provided", exception.Message);
            httpFactory.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendAsync_ReturnsSuccessfulResponse_WhenApiCallSucceeds()
        {
            // Arrange
            var capturedRequest = new List<HttpRequestMessage>();
            var apiHandler = new DelegatingHandlerStub(async (request, cancellationToken) =>
            {
                capturedRequest.Add(request);
                var responseBody = JsonSerializer.Serialize(new { data = new[] { new { id = "u1" } } });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                };
            });
            var tokenHandler = new DelegatingHandlerStub((request, cancellationToken) =>
            {
                var responseBody = JsonSerializer.Serialize(new { access_token = "token" });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
            });
            var httpFactory = CreateHttpFactory(apiHandler, tokenHandler);
            var options = CreateOptions();
            var logger = new Mock<ILogger<ZohoCrmConnection>>();
            var sut = new ZohoCrmConnection(httpFactory.Object, options, logger.Object);

            // Act
            var response = await sut.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("u1", content);
            Assert.Single(capturedRequest);
            var firstRequest = capturedRequest[0];
            Assert.Equal(HttpMethod.Get, firstRequest.Method);
            Assert.Equal("/crm/v2/users", firstRequest.RequestUri!.PathAndQuery);
            httpFactory.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            httpFactory.Verify(f => f.CreateClient("zoho_token"), Times.Once());
            httpFactory.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendAsync_PropagatesException_WhenApiCallFails()
        {
            // Arrange
            var apiHandler = new DelegatingHandlerStub((request, cancellationToken) =>
            {
                throw new HttpRequestException("api failure");
            });
            var tokenHandler = new DelegatingHandlerStub((request, cancellationToken) =>
            {
                var responseBody = JsonSerializer.Serialize(new { access_token = "token" });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
            });
            var httpFactory = CreateHttpFactory(apiHandler, tokenHandler);
            var options = CreateOptions();
            var logger = new Mock<ILogger<ZohoCrmConnection>>();
            var sut = new ZohoCrmConnection(httpFactory.Object, options, logger.Object);

            // Act
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.SendAsync(HttpMethod.Post, "/crm/v2/users", "{}", CancellationToken.None));

            // Assert
            Assert.Equal("api failure", exception.Message);
            httpFactory.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            httpFactory.Verify(f => f.CreateClient("zoho_token"), Times.Once());
            httpFactory.VerifyNoOtherCalls();
        }

        private static Mock<IHttpClientFactory> CreateHttpFactory(DelegatingHandlerStub apiHandler, DelegatingHandlerStub tokenHandler)
        {
            var apiClient = new HttpClient(apiHandler)
            {
                BaseAddress = new Uri("https://example.com")
            };
            var tokenClient = new HttpClient(tokenHandler)
            {
                BaseAddress = new Uri("https://example.com")
            };

            var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            factory.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factory.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);
            return factory;
        }

        private static ZohoOptions CreateOptions()
        {
            return new ZohoOptions();
        }

        private sealed class DelegatingHandlerStub : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

            public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _handler(request, cancellationToken);
            }
        }
    }
}