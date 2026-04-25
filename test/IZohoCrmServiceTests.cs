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
        public async Task PublicMethods_AreDeclared_OnInterface()
        {
            // Arrange
            var type = typeof(IZohoCrmService);

            // Act
            var methods = type.GetMethods();

            // Assert
            Assert.Equal(3, methods.Length);
            Assert.Contains(methods, m => m.Name == "GetUsersAsync");
            Assert.Contains(methods, m => m.Name == "CreateUserAsync");
            Assert.Contains(methods, m => m.Name == "UpdateUserAsync");
        }

        [Fact]
        public void InterfaceMethods_HaveExpectedSignatures()
        {
            // Arrange
            var type = typeof(IZohoCrmService);

            // Act
            var createMethod = type.GetMethod("CreateUserAsync");
            var updateMethod = type.GetMethod("UpdateUserAsync");

            // Assert
            Assert.NotNull(createMethod);
            Assert.NotNull(updateMethod);
            Assert.Equal(typeof(Task<object>), createMethod!.ReturnType);
            Assert.Equal(typeof(Task<object>), updateMethod!.ReturnType);
        }
    }
}
