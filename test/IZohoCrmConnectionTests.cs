// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class IZohoCrmConnectionTests
    {
        [Fact]
        public async Task SendAsync_CanBeImplementedForSuccessPath()
        {
            // Arrange
            var method = HttpMethod.Get;
            var relativePath = "/crm/v2/users";
            var body = (string?)null;
            var token = CancellationToken.None;
            var expected = new HttpResponseMessage(HttpStatusCode.OK);
            var connection = new StubConnection((m, p, b, c) =>
            {
                Assert.Equal(method, m);
                Assert.Equal(relativePath, p);
                Assert.Equal(body, b);
                Assert.Equal(token, c);
                return Task.FromResult(expected);
            });

            // Act
            var response = await connection.SendAsync(method, relativePath, body, token);

            // Assert
            Assert.Same(expected, response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_CanBeImplementedForFailurePath()
        {
            // Arrange
            var method = HttpMethod.Post;
            var relativePath = "/crm/v2/users";
            var body = "payload";
            var token = CancellationToken.None;
            var exception = new InvalidOperationException("connection failed");
            var connection = new StubConnection((m, p, b, c) =>
            {
                Assert.Equal(method, m);
                Assert.Equal(relativePath, p);
                Assert.Equal(body, b);
                Assert.Equal(token, c);
                return Task.FromException<HttpResponseMessage>(exception);
            });

            // Act
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SendAsync(method, relativePath, body, token));

            // Assert
            Assert.Equal("connection failed", thrown.Message);
        }

        private sealed class StubConnection : IZohoCrmConnection
        {
            private readonly Func<HttpMethod, string, string?, CancellationToken, Task<HttpResponseMessage>> _handler;

            public StubConnection(Func<HttpMethod, string, string?, CancellationToken, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
            {
                return _handler(method, relativePath, body, cancellationToken);
            }
        }
    }
}