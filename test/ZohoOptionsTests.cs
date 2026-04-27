// GENERATED_BY_AI_TEST_ENGINE
using Xunit;
using ZohoProject2.Models;

namespace ZohoProject2.Tests.Models
{
    public class ZohoOptionsTests
    {
        [Fact]
        public void ZohoOptions_AllowsSettingAndReadingProperties()
        {
            var options = new ZohoOptions
            {
                ClientId = "client-id",
                ClientSecret = "client-secret",
                TokenUrl = "https://accounts.example.com/oauth/v2/token",
                RefreshToken = "refresh-token",
                BaseUrl = "https://api.example.com",
                Port = 8080,
                Provider = "AZURE"
            };

            Assert.Equal("client-id", options.ClientId);
            Assert.Equal("client-secret", options.ClientSecret);
            Assert.Equal("https://accounts.example.com/oauth/v2/token", options.TokenUrl);
            Assert.Equal("refresh-token", options.RefreshToken);
            Assert.Equal("https://api.example.com", options.BaseUrl);
            Assert.Equal(8080, options.Port);
            Assert.Equal("AZURE", options.Provider);
        }

        [Fact]
        public void ZohoOptions_Defaults_AreNullOrEmpty()
        {
            var options = new ZohoOptions();

            Assert.Null(options.ClientId);
            Assert.Null(options.ClientSecret);
            Assert.Null(options.TokenUrl);
            Assert.Null(options.RefreshToken);
            Assert.Null(options.BaseUrl);
            Assert.Equal(0, options.Port);
            Assert.Null(options.Provider);
        }
    }
}