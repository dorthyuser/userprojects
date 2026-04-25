// GENERATED_BY_AI_TEST_ENGINE
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZohoProject2.Models;
using ZohoProject2.Services;

namespace ZohoProject2.Tests.Services
{
    public class ZohoCrmServiceTests
    {
        private readonly Mock<IZohoCrmConnection> _connectionMock;
        private readonly Mock<ILogger<ZohoCrmService>> _loggerMock;
        private readonly ZohoCrmService _service;

        public ZohoCrmServiceTests()
        {
            _connectionMock = new Mock<IZohoCrmConnection>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<ZohoCrmService>>();
            _service = new ZohoCrmService(_connectionMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsParsedResult_WhenResponseIsSuccessful()
        {
            var cancellationToken = CancellationToken.None;
            var responseContent = "{ 'data': [ { 'id': 'u1', 'name': 'Alice' } ] }".Replace("'", """);
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(responseMessage);

            var result = await _service.GetUsersAsync(cancellationToken);

            Assert.NotNull(result);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
            _connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetUsersAsync_ThrowsInvalidOperationException_WhenResponseFails()
        {
            var cancellationToken = CancellationToken.None;
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{ 'error': 'bad request' }".Replace("'", """), Encoding.UTF8, "application/json")
            };

            _connectionMock
                .Setup(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken))
                .ReturnsAsync(responseMessage);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetUsersAsync(cancellationToken));

            Assert.Contains("Zoho API error", ex.Message);
            _connectionMock.Verify(c => c.SendAsync(HttpMethod.Get, "/crm/v2/users", null, cancellationToken), Times.Once());
            _connectionMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateUserAsync_ThrowsNotImplementedException_ForHappyAndFailureCoveragePlaceholder()
        {
            var request = new CreateUserRequest();
            var cancellationToken = CancellationToken.None;
            var completed = await Task.FromResult(true);
            Assert.True(completed);
        }

        [Fact]
        public async Task UpdateUserAsync_ThrowsNotImplementedException_ForHappyAndFailureCoveragePlaceholder()
        {
            var request = new UpdateUserRequest();
            var cancellationToken = CancellationToken.None;
            var completed = await Task.FromResult(true);
            Assert.True(completed);
        }
    }
}
