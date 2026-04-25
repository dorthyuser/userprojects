// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        public void Constructor_ThrowsWhenOptionsAreNull()
        {
            var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            factoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(new HttpClient(new HttpClientHandler()));
            factoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(new HttpClient(new HttpClientHandler()));

            var ex = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(factoryMock.Object, null, loggerMock.Object));

            Assert.Equal("ZohoOptions not provided", ex.Message);
            factoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            factoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once());
        }

        [Fact]
        public void Constructor_CreatesClientsAndStoresDependencies_WhenOptionsProvided()
        {
            var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<ZohoCrmConnection>>();
            var options = new ZohoOptions();
            var apiClient = new HttpClient(new HttpClientHandler());
            var tokenClient = new HttpClient(new HttpClientHandler());
            factoryMock.Setup(f => f.CreateClient("zoho_api")).Returns(apiClient);
            factoryMock.Setup(f => f.CreateClient("zoho_token")).Returns(tokenClient);

            var connection = new ZohoCrmConnection(factoryMock.Object, options, loggerMock.Object);

            Assert.NotNull(connection);
            factoryMock.Verify(f => f.CreateClient("zoho_api"), Times.Once());
            factoryMock.Verify(f => f.CreateClient("zoho_token"), Times.Once());
            factoryMock.VerifyNoOtherCalls();
        }
    }
}
