// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class IZohoCrmConnectionTests
    {
        [Fact]
        public async Task SendAsync_InterfaceSignature_IsAvailable()
        {
            var mock = new Mock<IZohoCrmConnection>();
            var method = HttpMethod.Get;
            var path = "/test";
            string? body = null;
            var token = CancellationToken.None;
            mock.Setup(m => m.SendAsync(method, path, body, token)).ThrowsAsync(new InvalidOperationException("not implemented"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => mock.Object.SendAsync(method, path, body, token));
            mock.Verify(m => m.SendAsync(method, path, body, token), Times.Once);
        }

        [Fact]
        public async Task SendAsync_InterfaceSignature_HandlesBodyAndCancellationToken()
        {
            var mock = new Mock<IZohoCrmConnection>();
            var method = HttpMethod.Post;
            var path = "/users";
            var body = JsonSerializer.Serialize(new { name = "A" });
            var token = new CancellationToken(true);
            mock.Setup(m => m.SendAsync(method, path, body, token)).ThrowsAsync(new ArgumentException("invalid"));

            await Assert.ThrowsAsync<ArgumentException>(() => mock.Object.SendAsync(method, path, body, token));
            mock.Verify(m => m.SendAsync(method, path, body, token), Times.Once);
        }
    }
}
