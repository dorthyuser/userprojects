using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResponseHttp.Services;
using Xunit;

namespace ResponseHttp.Tests
{
    public class HttpClientExtensionsTests
    {
        [Fact]
        public void AddHttpConnection_ConfiguresBaseAddress_WhenProviderAndPortPresent()
        {
            // Arrange
            var services = new ServiceCollection();

            var inMemory = new Dictionary<string, string>
            {
                ["Http:Provider"] = "https://example.com",
                ["Http:Port"] = "443",
                // BasicAuthHandler will be registered but won't be invoked here
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();

            // Act
            services.AddHttpConnection(configuration);
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<System.Net.Http.IHttpClientFactory>();
            var client = factory.CreateClient("http");

            // Assert
            Assert.NotNull(client.BaseAddress);
            Assert.Equal(new Uri("https://example.com:443"), client.BaseAddress);
        }
    }
}
