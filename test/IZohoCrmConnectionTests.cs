// GENERATED_BY_AI_TEST_ENGINE
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
        public async Task SendAsync_ReturnsHttpResponseMessage_WhenImplementedMockSucceeds()
        {
            // Arrange
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            var expectedResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await connectionMock.Object.SendAsync(HttpMethod.Get, "/crm/v2/users", null, CancellationToken.None);

            // Assert
            Assert.Same(expectedResponse, result);
            connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task SendAsync_Throws_WhenCallNotConfigured()
        {
            // Arrange
            var connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);

            // Act & Assert
            await Assert.ThrowsAsync<MockException>(async () =>
            {
                await connectionMock.Object.SendAsync(HttpMethod.Post, "/crm/v2/users", null, CancellationToken.None);
            });
        }
    }
}