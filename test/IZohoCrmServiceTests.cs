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
        public void Interface_ShouldDefineAllPublicMethods()
        {
            var getUsers = typeof(IZohoCrmService).GetMethod("GetUsersAsync");
            var createUser = typeof(IZohoCrmService).GetMethod("CreateUserAsync");
            var updateUser = typeof(IZohoCrmService).GetMethod("UpdateUserAsync");

            Assert.NotNull(getUsers);
            Assert.NotNull(createUser);
            Assert.NotNull(updateUser);
            Assert.Equal(typeof(Task<object>), getUsers.ReturnType);
            Assert.Equal(typeof(Task<object>), createUser.ReturnType);
            Assert.Equal(typeof(Task<object>), updateUser.ReturnType);
        }

        [Fact]
        public void Interface_ShouldUseExpectedMethodSignatures()
        {
            var createUser = typeof(IZohoCrmService).GetMethod("CreateUserAsync");
            var updateUser = typeof(IZohoCrmService).GetMethod("UpdateUserAsync");

            Assert.NotNull(createUser);
            Assert.NotNull(updateUser);
            var createParams = createUser.GetParameters();
            var updateParams = updateUser.GetParameters();

            Assert.Equal(2, createParams.Length);
            Assert.Equal(typeof(CreateUserRequest), createParams[0].ParameterType);
            Assert.Equal(typeof(CancellationToken), createParams[1].ParameterType);
            Assert.Equal(3, updateParams.Length);
            Assert.Equal(typeof(string), updateParams[0].ParameterType);
            Assert.Equal(typeof(UpdateUserRequest), updateParams[1].ParameterType);
            Assert.Equal(typeof(CancellationToken), updateParams[2].ParameterType);
        }
    }
}
