// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class IZohoCrmServiceTests
    {
        [Fact]
        public async Task Interface_Definition_Contains_All_Public_Methods()
        {
            var getUsers = typeof(IZohoCrmService).GetMethod("GetUsersAsync");
            var createUser = typeof(IZohoCrmService).GetMethod("CreateUserAsync");
            var updateUser = typeof(IZohoCrmService).GetMethod("UpdateUserAsync");

            Assert.NotNull(getUsers);
            Assert.NotNull(createUser);
            Assert.NotNull(updateUser);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Interface_Definition_Method_Count_Is_Three()
        {
            var methods = typeof(IZohoCrmService).GetMethods();
            var count = methods.Length;
            Assert.Equal(3, count);
            await Task.CompletedTask;
        }
    }
}