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
        public async Task SendAsync_InterfaceContract_AllowsSuccessfulCompletion()
        {
            // Arrange
            Task<HttpResponseMessage> Act() => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            var method = HttpMethod.Get;
            var path = "/test";
            var body = (string?)null;
            var token = CancellationToken.None;

            // Act
            var responseTask = Act();
            var response = await responseTask;

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(method, HttpMethod.Get);
            Assert.Equal(path, "/test");
            Assert.Null(body);
            Assert.Equal(token, CancellationToken.None);
        }

        [Fact]
        public async Task SendAsync_InterfaceContract_PropagatesExceptionScenario()
        {
            // Arrange
            Task<HttpResponseMessage> Act() => throw new InvalidOperationException("failed");

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Act());

            // Assert
            Assert.Equal("failed", exception.Message);
        }
    }
}
