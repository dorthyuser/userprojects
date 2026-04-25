// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using System.Text.Json;
using Xunit;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Tests
{
    public class IZohoCrmConnectionTests
    {
        [Fact]
        public async Task SendAsync_ReturnsHttpResponseMessage_OnSuccess()
        {
            // Arrange
            var mock = new Mock<IZohoCrmConnection>();
            var payload = new { ok = true };
            var json = JsonSerializer.Serialize(payload);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
            mock.Setup(m => m.SendAsync(HttpMethod.Get, "/path", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await mock.Object.SendAsync(HttpMethod.Get, "/path", null, CancellationToken.None);
            var content = await result.Content.ReadAsStringAsync(CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(json, content);
            mock.Verify(m => m.SendAsync(HttpMethod.Get, "/path", null, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task SendAsync_ThrowsException_WhenConfigured()
        {
            // Arrange
            var mock = new Mock<IZohoCrmConnection>();
            mock.Setup(m => m.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("fail"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => mock.Object.SendAsync(HttpMethod.Get, "p", null, CancellationToken.None));
            mock.Verify(m => m.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
