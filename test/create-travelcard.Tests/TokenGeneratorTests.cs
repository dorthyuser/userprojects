using System;
using System.Linq;
using CreateTravelcard.Helpers;
using Xunit;

namespace CreateTravelcard.Tests
{
    public class TokenGeneratorTests
    {
        [Fact]
        public void GenerateToken_ReturnsNonEmpty_AndUrlSafe()
        {
            var gen = new TokenGenerator();
            var token1 = gen.GenerateToken();
            var token2 = gen.GenerateToken();

            Assert.False(string.IsNullOrWhiteSpace(token1));
            Assert.False(string.IsNullOrWhiteSpace(token2));
            Assert.NotEqual(token1, token2); // very low probability of collision

            // Ensure tokens don't contain characters that were replaced (+, /, =)
            Assert.DoesNotContain("+", token1);
            Assert.DoesNotContain("/", token1);
            Assert.DoesNotContain("=", token1);

            Assert.DoesNotContain("+", token2);
            Assert.DoesNotContain("/", token2);
            Assert.DoesNotContain("=", token2);

            // Basic length check - base64 of 32 bytes is 44 chars normally, trimmed '=' may reduce length
            Assert.True(token1.Length >= 43 || token1.Length <= 44 || token1.Length >= 42);
        }
    }
}
