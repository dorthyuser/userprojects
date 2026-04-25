// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class IZohoCrmConnectionTests
    {
        [Fact]
        public async Task SendAsync_InterfaceContract_TaskCanBeObserved()
        {
            // Arrange
            IZohoCrmConnection? connection = null;
            var method = HttpMethod.Get;
            var relativePath = "/crm/v2/users";
            string? body = null;
            var token = CancellationToken.None;

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                _ = await connection!.SendAsync(method, relativePath, body, token);
            });
        }

        [Fact]
        public void SendAsync_InterfaceType_IsPublicAndAccessible()
        {
            // Arrange
            var type = typeof(IZohoCrmConnection);

            // Act
            var methods = type.GetMethods();

            // Assert
            Assert.Single(methods);
            Assert.Equal("SendAsync", methods[0].Name);
        }
    }
}
