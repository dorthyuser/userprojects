// GENERATED_BY_AI_TEST_ENGINE
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
        public async Task SendAsync_TaskCompletes_WhenImplementationsReturnResponse()
        {
            // Arrange
            IZohoCrmConnection connection = new FakeConnection();
            var method = HttpMethod.Get;
            var path = "/users";
            var body = (string?)null;
            var token = CancellationToken.None;

            // Act
            var response = await connection.SendAsync(method, path, body, token);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SendAsync_TaskCompletes_WithPostMethodAndBody()
        {
            // Arrange
            IZohoCrmConnection connection = new FakeConnection();
            var method = HttpMethod.Post;
            var path = "/users";
            var body = JsonPayload();
            var token = CancellationToken.None;

            // Act
            var response = await connection.SendAsync(method, path, body, token);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        private static string JsonPayload()
        {
            return "{'name':'Jane'}".Replace("'", """);
        }

        private sealed class FakeConnection : IZohoCrmConnection
        {
            public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }
    }
}
