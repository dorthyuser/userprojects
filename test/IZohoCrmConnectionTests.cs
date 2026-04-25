// GENERATED_BY_AI_TEST_ENGINE
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
        public async Task SendAsync_IsDeclaredWithExpectedSignature()
        {
            // Arrange
            var method = HttpMethod.Get;
            var path = "/crm/v2/users";
            var body = (string?)null;
            var cancellationToken = CancellationToken.None;

            // Act
            Task<HttpResponseMessage> Invoke(IZohoCrmConnection connection) => connection.SendAsync(method, path, body, cancellationToken);

            // Assert
            await Task.CompletedTask;
            Assert.NotNull(typeof(IZohoCrmConnection).GetMethod(nameof(IZohoCrmConnection.SendAsync)));
        }

        [Fact]
        public void SendAsync_InterfaceMethod_HasExpectedParameters()
        {
            // Arrange
            var methodInfo = typeof(IZohoCrmConnection).GetMethod(nameof(IZohoCrmConnection.SendAsync));

            // Act
            var parameters = methodInfo!.GetParameters();

            // Assert
            Assert.Equal(4, parameters.Length);
            Assert.Equal(typeof(HttpMethod), parameters[0].ParameterType);
            Assert.Equal(typeof(string), parameters[1].ParameterType);
            Assert.Equal(typeof(string), parameters[2].ParameterType);
            Assert.Equal(typeof(CancellationToken), parameters[3].ParameterType);
        }
    }
}
