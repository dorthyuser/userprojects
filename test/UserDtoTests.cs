// GENERATED_BY_AI_TEST_ENGINE
using Xunit;
using ZohoProject2.Models;

namespace ZohoProject2.Tests.Models
{
    public class UserDtoTests
    {
        [Fact]
        public void UserDto_AllowsPropertyAssignment()
        {
            var user = new UserDto
            {
                Id = "1",
                FullName = "Test User",
                Email = "test@example.com"
            };

            Assert.Equal("1", user.Id);
            Assert.Equal("Test User", user.FullName);
            Assert.Equal("test@example.com", user.Email);
        }

        [Fact]
        public void UserDto_DefaultValues_AreNull()
        {
            var user = new UserDto();

            Assert.Null(user.Id);
            Assert.Null(user.FullName);
            Assert.Null(user.Email);
        }
    }
}