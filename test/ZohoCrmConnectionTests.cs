// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Tests
{
    public class ZohoCrmConnectionTests
    {
        [Fact]
        public void Constructor_CreatesHttpClients_WhenDependenciesPresent()
        {
            // Arrange
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient("zoho-api")).Returns(new HttpClient());
            factory.Setup(f => f.CreateClient("zoho-token")).Returns(new HttpClient());
            var options = new ZohoOptions();
            var logger = new Mock<ILogger<ZohoCrmConnection>>();

            // Act
            var instance = new ZohoCrmConnection(factory.Object, options, logger.Object);

            // Assert
            Assert.NotNull(instance);
            factory.Verify(f => f.CreateClient("zoho-api"), Times.Once());
            factory.Verify(f => f.CreateClient("zoho-token"), Times.Once());
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenOptionsNull()
        {
            // Arrange
            var factory = new Mock<IHttpClientFactory>();
            var logger = new Mock<ILogger<ZohoCrmConnection>>();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ZohoCrmConnection(factory.Object, null!, logger.Object));
            Assert.Equal("options", ex.ParamName);
            factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never());
        }
    }
}
