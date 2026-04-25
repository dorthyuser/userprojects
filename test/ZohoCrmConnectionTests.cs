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
        public void Constructor_Throws_WhenOptionsIsNull()
        {
            // Arrange
            var httpFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            ZohoOptions? options = null;

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(httpFactoryMock.Object, options!, loggerMock.Object));

            // Assert
            Assert.Equal("ZohoOptions not provided", ex.Message);
            httpFactoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void Constructor_CreatesNamedClients_WhenOptionsProvided()
        {
            // Arrange
            var apiClient = new HttpClient(new HttpClientHandler());
            var tokenClient = new HttpClient(new HttpClientHandler());
            var httpFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions();

            httpFactoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            httpFactoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            // Act
            var connection = new ZohoCrmConnection(httpFactoryMock.Object, options, loggerMock.Object);

            // Assert
            Assert.NotNull(connection);
            httpFactoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            httpFactoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once());
            httpFactoryMock.VerifyNoOtherCalls();
        }
    }
}
