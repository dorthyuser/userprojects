using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using zoho_project_csharp.Services;
using zoho_project_csharp.Models;
using Microsoft.Extensions.Logging;

namespace zoho_project_csharp.Tests
{
    public class ZohoCrmServiceTests
    {
        private class FakeConnection : IZohoCrmConnection
        {
            public HttpMethod? LastMethod { get; private set; }
            public string? LastRelativePath { get; private set; }
            public string? LastBody { get; private set; }
            private readonly HttpResponseMessage _response;

            public FakeConnection(HttpResponseMessage response)
            {
                _response = response;
            }

            public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
            {
                LastMethod = method;
                LastRelativePath = relativePath;
                LastBody = body;
                // Return a cloned response to avoid disposed content issues across multiple awaits
                var msg = new HttpResponseMessage(_response.StatusCode)
                {
                    Content = _response.Content == null ? null : new StringContent(_response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), Encoding.UTF8, _response.Content.Headers.ContentType?.MediaType ?? "text/plain")
                };
                return Task.FromResult(msg);
            }
        }

        private class NoOpLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
            private class NullScope : IDisposable { public static NullScope Instance { get; } = new NullScope(); public void Dispose() { } }
        }

        [Fact]
        public async Task GetUsersAsync_UsesGetEndpoint_AndReturnsContent()
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{"users":[]}", Encoding.UTF8, "application/json") };
            var conn = new FakeConnection(resp);
            var svc = new ZohoCrmService(conn, new NoOpLogger<ZohoCrmService>());

            var (status, content) = await svc.GetUsersAsync(CancellationToken.None);

            Assert.Equal(200, status);
            Assert.Equal("{"users":[]}", content);
            Assert.Equal(HttpMethod.Get, conn.LastMethod);
            Assert.Equal("/crm/v2/users", conn.LastRelativePath);
            Assert.Null(conn.LastBody);
        }

        [Fact]
        public async Task CreateUserAsync_FlattensRoleAndProfileToStrings()
        {
            var resp = new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{"id":"u1"}", Encoding.UTF8, "application/json") };
            var conn = new FakeConnection(resp);
            var svc = new ZohoCrmService(conn, new NoOpLogger<ZohoCrmService>());

            var req = new CreateUserRequest
            {
                FirstName = "First",
                LastName = "Last",
                Email = "e@example.com",
                Role = new RoleRef { Id = "role-42" },
                Profile = new ProfileRef { Id = "profile-7" }
            };

            var (status, content) = await svc.CreateUserAsync(req, CancellationToken.None);

            Assert.Equal(201, status);
            Assert.Equal("{"id":"u1"}", content);
            Assert.Equal(HttpMethod.Post, conn.LastMethod);
            Assert.Equal("/crm/v2/users", conn.LastRelativePath);
            Assert.NotNull(conn.LastBody);
            Assert.Contains(""users":", conn.LastBody);
            Assert.Contains("role-42", conn.LastBody);
            Assert.Contains("profile-7", conn.LastBody);
            // Additional expected static fields
            Assert.Contains("country", conn.LastBody);
            Assert.Contains("time_zone", conn.LastBody);
        }

        [Fact]
        public async Task UpdateUserAsync_UsesPutWithEscapedId_AndPayloadContainsUpdates()
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{"ok":true}", Encoding.UTF8, "application/json") };
            var conn = new FakeConnection(resp);
            var svc = new ZohoCrmService(conn, new NoOpLogger<ZohoCrmService>());

            var req = new UpdateUserRequest
            {
                FirstName = "New",
                LastName = "Name",
                Role = new RoleRef { Id = "r1" }
            };

            var (status, content) = await svc.UpdateUserAsync("id with spaces", req, CancellationToken.None);

            Assert.Equal(200, status);
            Assert.Equal("{"ok":true}", content);
            Assert.Equal(HttpMethod.Put, conn.LastMethod);
            Assert.NotNull(conn.LastRelativePath);
            Assert.Contains("/crm/v2/users/", conn.LastRelativePath!);
            Assert.Contains(Uri.EscapeDataString("id with spaces"), conn.LastRelativePath);
            Assert.NotNull(conn.LastBody);
            Assert.Contains("New", conn.LastBody);
            Assert.Contains("r1", conn.LastBody);
        }
    }
}
