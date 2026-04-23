using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Xunit;
using zoho_project_csharp.Services;
using zoho_project_csharp.Models;
using Microsoft.Extensions.Logging;

namespace zoho_project_csharp.Tests
{
    public class ZohoCrmServiceTests
    {
        private class CapturedRequest
        {
            public HttpMethod? Method { get; set; }
            public string? RelativePath { get; set; }
            public string? Body { get; set; }
        }

        private class FakeConnection : IZohoCrmConnection
        {
            private readonly CapturedRequest _captured;
            private readonly HttpResponseMessage _response;

            public FakeConnection(CapturedRequest captured, HttpResponseMessage response)
            {
                _captured = captured;
                _response = response;
            }

            public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
            {
                _captured.Method = method;
                _captured.RelativePath = relativePath;
                _captured.Body = body;

                // Return a copy to mimic typical behavior
                var clone = new HttpResponseMessage(_response.StatusCode)
                {
                    Content = _response.Content == null ? null : new StringContent(_response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), Encoding.UTF8, _response.Content.Headers.ContentType?.MediaType)
                };
                return Task.FromResult(clone);
            }
        }

        // Minimal no-op logger to avoid test dependency on logging abstractions
        private class NoOpLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

            private class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new NullScope();
                public void Dispose() { }
            }
        }

        [Fact]
        public async Task CreateUserAsync_SerializesFlattenedRoleAndProfile_AndReturnsResponse()
        {
            var captured = new CapturedRequest();
            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{"ok":true}", Encoding.UTF8, "application/json")
            };

            var conn = new FakeConnection(captured, response);
            var service = new ZohoCrmService(conn, new NoOpLogger<ZohoCrmService>());

            var request = new CreateUserRequest
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Role = new RoleRef { Id = "role-id" },
                Profile = new ProfileRef { Id = "profile-id" }
            };

            var (status, content) = await service.CreateUserAsync(request, CancellationToken.None);

            Assert.Equal(201, status);
            Assert.Equal("{"ok":true}", content);
            Assert.Equal(HttpMethod.Post, captured.Method);
            Assert.Equal("/crm/v2/users", captured.RelativePath);
            Assert.NotNull(captured.Body);
            Assert.Contains(""role":"role-id"", captured.Body);
            Assert.Contains(""profile":"profile-id"", captured.Body);
            Assert.Contains(""first_name":"John"", captured.Body);
            Assert.Contains(""last_name":"Doe"", captured.Body);
            Assert.Contains(""email":"john.doe@example.com"", captured.Body);
        }

        [Fact]
        public async Task UpdateUserAsync_EscapesIdAndReturnsResponse()
        {
            var captured = new CapturedRequest();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{"updated":true}", Encoding.UTF8, "application/json")
            };

            var conn = new FakeConnection(captured, response);
            var service = new ZohoCrmService(conn, new NoOpLogger<ZohoCrmService>());

            var request = new UpdateUserRequest
            {
                FirstName = "Jane",
                LastName = "Roe",
                Email = "jane.roe@example.com",
                Role = new RoleRef { Id = "r1" },
                Profile = new ProfileRef { Id = "p1" }
            };

            // Use an id that requires escaping
            var id = "user/1";

            var (status, content) = await service.UpdateUserAsync(id, request, CancellationToken.None);

            Assert.Equal(200, status);
            Assert.Equal("{"updated":true}", content);
            Assert.Equal(HttpMethod.Put, captured.Method);
            // Should be escaped
            Assert.Equal("/crm/v2/users/user%2F1", captured.RelativePath);
            Assert.NotNull(captured.Body);
            Assert.Contains(""role":"r1"", captured.Body);
            Assert.Contains(""profile":"p1"", captured.Body);
        }
    }
}
