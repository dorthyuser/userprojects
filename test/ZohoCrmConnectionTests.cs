// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Xunit;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class ZohoCrmConnectionTests
    {
        [Fact]
        public void Constructor_Throws_WhenOptionsAreNull()
        {
            // Arrange
            Microsoft.Extensions.Logging.ILogger<ZohoCrmConnection> logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ZohoCrmConnection>();
            IHttpClientFactory factory = new StubHttpClientFactory();

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => new ZohoCrmConnection(factory, null!, logger));
            string message = exception.Message;

            // Assert
            Assert.Equal("ZohoOptions not provided", message);
        }

        [Fact]
        public void Constructor_CreatesInstance_WhenOptionsProvided()
        {
            // Arrange
            Microsoft.Extensions.Logging.ILogger<ZohoCrmConnection> logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ZohoCrmConnection>();
            IHttpClientFactory factory = new StubHttpClientFactory();
            var options = new ZohoProject2.Models.ZohoOptions();

            // Act
            var connection = new ZohoCrmConnection(factory, options, logger);
            var typeName = connection.GetType().Name;

            // Assert
            Assert.Equal("ZohoCrmConnection", typeName);
        }

        private sealed class StubHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                return new HttpClient(new HttpClientHandler(), disposeHandler: true);
            }
        }
    }
}