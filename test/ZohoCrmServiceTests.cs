using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using zoho_project_csharp.Models;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Tests
{
    public class ZohoCrmServiceTests
    {
        private class StubConnection : IZohoCrmConnection
        {
            public HttpMethod? LastMethod { get; private set; }
            public string? LastPath { get; private set; }
            public string? LastBody { get; private set; }
            private readonly HttpResponseMessage _response;

            public StubConnection(HttpResponseMessage response) => _response = response;

            public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
            {
                LastMethod = method;
                LastPath = relativePath;
                LastBody = body;
                // Return a fresh clone so caller can dispose
                var clone = new HttpResponseMessage(_response.StatusCode)
                {
                    Content = _response.Content == null ? null : new StringContent(_response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), Encoding.UTF8, _response.Content.Headers.ContentType?.MediaType ?? "text/plain")
                };
                return Task.FromResult(clone);
            }
        }

        private class NoopLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter) { }
            private class NullScope : System.IDisposable { public static NullScope Instance { get; } = new NullScope(); public void Dispose() { } }
        }

        [Fact]
        public async Task CreateUserAsync_SerializesFlattenedRoleAndProfile_AndReturnsResponse()
        {
            // Arrange
            var apiResponse = new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{"id":"u1"}", Encoding.UTF8, "application/json") };
            var stub = new StubConnection(apiResponse);
            var svc = new ZohoCrmService(stub, new NoopLogger<ZohoCrmService>());

            var req = new CreateUserRequest
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Role = new RoleRef { Id = "role-id" },
                Profile = new ProfileRef { Id = "profile-id" }
            };

            // Act
            var (status, content) = await svc.CreateUserAsync(req, CancellationToken.None);

            // Assert
            Assert.Equal((int)HttpStatusCode.Created, status);
            Assert.Equal("{"id":"u1"}", content);
            Assert.NotNull(stub.LastBody);
            Assert.Contains("users", stub.LastBody);
            Assert.Contains(""role":"role-id"", stub.LastBody);
            Assert.Contains(""profile":"profile-id"", stub.LastBody);
        }

        [Fact]
        public async Task UpdateUserAsync_UsesEscapedPath_AndReturnsResponse()
        {
            // Arrange
            var apiResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{"updated":true}", Encoding.UTF8, "application/json") };
            var stub = new StubConnection(apiResponse);
            var svc = new ZohoCrmService(stub, new NoopLogger<ZohoCrmService>());

            var req = new UpdateUserRequest
            {
                FirstName = "Jane",
                Role = new RoleRef { Id = "r2" }
            };

            var userId = "user/with slash";

            // Act
            var (status, content) = await svc.UpdateUserAsync(userId, req, CancellationToken.None);

            // Assert
            Assert.Equal((int)HttpStatusCode.OK, status);
            Assert.Equal("{"updated":true}", content);
            Assert.NotNull(stub.LastPath);
            Assert.Contains("/crm/v2/users/", stub.LastPath);
            Assert.Contains(Uri.EscapeDataString(userId), stub.LastPath);
            Assert.NotNull(stub.LastBody);
            Assert.Contains("users", stub.LastBody);
            Assert.Contains(""role":"r2"", stub.LastBody);
        }
    }
}
