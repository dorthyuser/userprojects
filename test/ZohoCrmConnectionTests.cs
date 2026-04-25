// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net.Http;
using System.Threading;
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
        public void Constructor_ThrowsInvalidOperationException_WhenOptionsAreNull()
        {
            // Arrange
            var factoryMock = new Mock<IHttpClientFactory>();
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            ZohoOptions options = null!;

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(factoryMock.Object, options, loggerMock.Object));
            Assert.Equal("ZohoOptions not provided", ex.Message);
            factoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public void Constructor_CreatesNamedClients_WhenOptionsAreProvided()
        {
            // Arrange
            var apiClient = new HttpClient(new HttpClientHandler());
            var tokenClient = new HttpClient(new HttpClientHandler());
            var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions();

            factoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            // Act
            var connection = new ZohoCrmConnection(factoryMock.Object, options, loggerMock.Object);

            // Assert
            Assert.NotNull(connection);
            factoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            factoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }
    }
}