// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net.Http;
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
        public void Interface_DeclaresAllPublicMethods()
        {
            // Arrange
            var type = typeof(IZohoCrmService);

            // Act
            var methods = type.GetMethods();

            // Assert
            Assert.Contains(methods, m => m.Name == nameof(IZohoCrmService.GetUsersAsync));
            Assert.Contains(methods, m => m.Name == nameof(IZohoCrmService.CreateUserAsync));
            Assert.Contains(methods, m => m.Name == nameof(IZohoCrmService.UpdateUserAsync));
        }

        [Fact]
        public void PublicMethods_HaveExpectedParameterCounts()
        {
            // Arrange
            var getUsers = typeof(IZohoCrmService).GetMethod(nameof(IZohoCrmService.GetUsersAsync));
            var createUser = typeof(IZohoCrmService).GetMethod(nameof(IZohoCrmService.CreateUserAsync));
            var updateUser = typeof(IZohoCrmService).GetMethod(nameof(IZohoCrmService.UpdateUserAsync));

            // Act
            var getUsersParams = getUsers!.GetParameters();
            var createUserParams = createUser!.GetParameters();
            var updateUserParams = updateUser!.GetParameters();

            // Assert
            Assert.Equal(1, getUsersParams.Length);
            Assert.Equal(2, createUserParams.Length);
            Assert.Equal(3, updateUserParams.Length);
            Assert.Equal(typeof(CancellationToken), getUsersParams[0].ParameterType);
            Assert.Equal(typeof(CreateUserRequest), createUserParams[0].ParameterType);
            Assert.Equal(typeof(UpdateUserRequest), updateUserParams[1].ParameterType);
        }
    }
}
