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
        public async Task SendAsync_InterfaceContract_CanBeImplementedAndInvoked()
        {
            // Arrange
            IZohoCrmConnection connection = new FakeConnection();
            var method = HttpMethod.Get;
            var relativePath = "/crm/v2/users";
            var body = (string?)null;
            var cancellationToken = CancellationToken.None;

            // Act
            var response = await connection.SendAsync(method, relativePath, body, cancellationToken);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("ok", content);
        }

        [Fact]
        public async Task SendAsync_InterfaceContract_HandlesNonNullBody()
        {
            // Arrange
            IZohoCrmConnection connection = new FakeConnection();
            var method = HttpMethod.Post;
            var relativePath = "/crm/v2/users";
            var body = "payload";
            var cancellationToken = CancellationToken.None;

            // Act
            var response = await connection.SendAsync(method, relativePath, body, cancellationToken);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("ok", content);
        }

        private sealed class FakeConnection : IZohoCrmConnection
        {
            public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("ok")
                };

                return Task.FromResult(response);
            }
        }
    }
}
