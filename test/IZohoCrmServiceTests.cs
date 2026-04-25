// GENERATED_BY_AI_TEST_ENGINE
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZohoProject2.Models;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class IZohoCrmServiceTests
    {
        [Fact]
        public void InterfaceHasExpectedMembers()
        {
            // Arrange
            var type = typeof(IZohoCrmService);

            // Act
            var methods = type.GetMethods();

            // Assert
            Assert.Equal(3, methods.Length);
        }

        [Fact]
        public void MethodSignatures_AreAsExpected()
        {
            // Arrange
            var getUsers = typeof(IZohoCrmService).GetMethod("GetUsersAsync");
            var createUser = typeof(IZohoCrmService).GetMethod("CreateUserAsync");
            var updateUser = typeof(IZohoCrmService).GetMethod("UpdateUserAsync");

            // Act
            var getUsersReturnType = getUsers!.ReturnType;
            var createUserReturnType = createUser!.ReturnType;
            var updateUserReturnType = updateUser!.ReturnType;

            // Assert
            Assert.Equal(typeof(Task<object>), getUsersReturnType);
            Assert.Equal(typeof(Task<object>), createUserReturnType);
            Assert.Equal(typeof(Task<object>), updateUserReturnType);
            Assert.Single(createUser.GetParameters());
            Assert.Equal(2, updateUser.GetParameters().Length);
        }
    }
}
