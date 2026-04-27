// GENERATED_BY_AI_TEST_ENGINE
using Xunit;
using ZohoProject2.Models;

namespace ZohoProject2.Tests.Models
{
    public class ZohoTokenResponseTests
    {
        [Fact]
        public void ZohoTokenResponse_AllowsReadingAndWritingProperties()
        {
            var token = new ZohoTokenResponse
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresIn = 3600,
                Status = "success",
                ApiDomain = "https://api.example.com"
            };

            Assert.Equal("access-token", token.AccessToken);
            Assert.Equal("refresh-token", token.RefreshToken);
            Assert.Equal(3600, token.ExpiresIn);
            Assert.Equal("success", token.Status);
            Assert.Equal("https://api.example.com", token.ApiDomain);
        }

        [Fact]
        public void ZohoTokenResponse_DefaultValues_AreUnset()
        {
            var token = new ZohoTokenResponse();

            Assert.Null(token.AccessToken);
            Assert.Null(token.RefreshToken);
            Assert.Null(token.ExpiresIn);
            Assert.Null(token.Status);
            Assert.Null(token.ApiDomain);
        }
    }
}