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
        public void InterfaceHasExpectedMembers()
        {
            // Arrange
            var type = typeof(IZohoCrmConnection);

            // Act
            var methods = type.GetMethods();

            // Assert
            Assert.Single(methods);
            Assert.Equal("SendAsync", methods[0].Name);
        }

        [Fact]
        public void SendAsyncSignature_IsAsExpected()
        {
            // Arrange
            var method = typeof(IZohoCrmConnection).GetMethod("SendAsync");

            // Act
            var parameters = method!.GetParameters();

            // Assert
            Assert.Equal(typeof(Task<HttpResponseMessage>), method.ReturnType);
            Assert.Equal(4, parameters.Length);
            Assert.Equal(typeof(HttpMethod), parameters[0].ParameterType);
            Assert.Equal(typeof(string), parameters[1].ParameterType);
            Assert.Equal(typeof(string), parameters[2].ParameterType);
            Assert.Equal(typeof(CancellationToken), parameters[3].ParameterType);
        }
    }
}
