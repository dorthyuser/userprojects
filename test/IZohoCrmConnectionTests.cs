// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZohoProject2.Services;

namespace ZohoProject2.Tests
{
    public class IZohoCrmConnectionTests
    {
        [Fact]
        public async Task SendAsync_ReturnsResponseTask_WhenImplementationProvided()
        {
            // Arrange
            IZohoCrmConnection connection = new FakeConnection();
            var method = HttpMethod.Get;
            var path = "/crm/v2/users";
            var body = (string?)null;
            var cancellationToken = CancellationToken.None;

            // Act
            var response = await connection.SendAsync(method, path, body, cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("ok", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SendAsync_PropagatesCancellation_WhenCancellationRequested()
        {
            // Arrange
            IZohoCrmConnection connection = new FakeConnection();
            var method = HttpMethod.Post;
            var path = "/crm/v2/users";
            var body = "payload";
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var response = await connection.SendAsync(method, path, body, cts.Token);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("cancelled", await response.Content.ReadAsStringAsync());
        }

        private sealed class FakeConnection : IZohoCrmConnection
        {
            public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("cancelled")
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("ok")
                });
            }
        }
    }
}
