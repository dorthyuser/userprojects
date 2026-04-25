// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net.Http;
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
        public async Task SendAsync_InterfaceContract_CanBeMockedForSuccess()
        {
            // Arrange
            var mock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var method = HttpMethod.Get;
            var relativePath = "/crm/v2/users";
            var body = (string?)null;
            var cancellationToken = CancellationToken.None;
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            mock.Setup(m => m.SendAsync(method, relativePath, body, cancellationToken)).ReturnsAsync(response);

            // Act
            var result = await mock.Object.SendAsync(method, relativePath, body, cancellationToken);

            // Assert
            Assert.Same(response, result);
            mock.Verify(m => m.SendAsync(method, relativePath, body, cancellationToken), Times.Once);
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendAsync_InterfaceContract_CanBeMockedForException()
        {
            // Arrange
            var mock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var method = HttpMethod.Post;
            var relativePath = "/crm/v2/users";
            var body = "payload";
            var cancellationToken = CancellationToken.None;
            mock.Setup(m => m.SendAsync(method, relativePath, body, cancellationToken)).ThrowsAsync(new InvalidOperationException("error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await mock.Object.SendAsync(method, relativePath, body, cancellationToken));
            mock.Verify(m => m.SendAsync(method, relativePath, body, cancellationToken), Times.Once);
            mock.VerifyNoOtherCalls();
        }
    }
}
