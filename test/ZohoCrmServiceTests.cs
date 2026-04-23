using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using zoho_project_csharp.Services;
using zoho_project_csharp.Models;

namespace zoho_project_csharp.Tests
{
    internal class CapturingConnection : IZohoCrmConnection
    {
        public HttpMethod? LastMethod { get; private set; }
        public string? LastRelativePath { get; private set; }
        public string? LastBody { get; private set; }

        private readonly HttpResponseMessage _response;

        public CapturingConnection(HttpResponseMessage response)
        {
            _response = response;
        }

        public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
        {
            LastMethod = method;
            LastRelativePath = relativePath;
            LastBody = body;
            // Return a cloned HttpResponseMessage so callers can dispose safely
            var clone = new HttpResponseMessage(_response.StatusCode)
            {
                Content = new StringContent(_response.Content?.ReadAsStringAsync(cancellationToken).Result ?? string.Empty, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(clone);
        }
    }

    public class ZohoCrmServiceTests
    {
        [Fact]
        public async Task GetUsers_Calls_Connection_With_Get_And_Correct_Path()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{"users":[]}", Encoding.UTF8, "application/json")
            };

            var conn = new CapturingConnection(response);
            var service = new ZohoCrmService(conn, new FakeLogger<ZohoCrmService>());

            var (status, content) = await service.GetUsersAsync(CancellationToken.None);

            Assert.Equal(200, status);
            Assert.Equal("{"users":[]}", content);
            Assert.Equal(HttpMethod.Get, conn.LastMethod);
            Assert.Equal("/crm/v2/users", conn.LastRelativePath);
            Assert.Null(conn.LastBody);
        }

        [Fact]
        public async Task CreateUser_Serializes_Request_With_Flattened_Role_And_Profile()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{"result":true}", Encoding.UTF8, "application/json")
            };

            var conn = new CapturingConnection(response);
            var service = new ZohoCrmService(conn, new FakeLogger<ZohoCrmService>());

            var req = new CreateUserRequest
            {
                FirstName = "Alpha",
                LastName = "Beta",
                Email = "alpha@beta.test",
                Role = new RoleRef { Id = "role-123" },
                Profile = new ProfileRef { Id = "profile-456" }
            };

            var (status, content) = await service.CreateUserAsync(req, CancellationToken.None);

            Assert.Equal(201, status);
            Assert.Equal("{"result":true}", content);

            Assert.Equal(HttpMethod.Post, conn.LastMethod);
            Assert.Equal("/crm/v2/users", conn.LastRelativePath);
            Assert.NotNull(conn.LastBody);

            using var doc = JsonDocument.Parse(conn.LastBody!);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("users", out var users));
            Assert.Equal(JsonValueKind.Array, users.ValueKind);
            Assert.Equal(1, users.GetArrayLength());
            var first = users[0];
            Assert.Equal("Alpha", first.GetProperty("first_name").GetString());
            Assert.Equal("Beta", first.GetProperty("last_name").GetString());
            Assert.Equal("alpha@beta.test", first.GetProperty("email").GetString());
            Assert.Equal("role-123", first.GetProperty("role").GetString());
            Assert.Equal("profile-456", first.GetProperty("profile").GetString());
            Assert.Equal("India", first.GetProperty("country").GetString());
        }

        [Fact]
        public async Task UpdateUser_Serializes_Request_And_Uses_Escaped_Id_In_Path()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{"ok":true}", Encoding.UTF8, "application/json")
            };

            var conn = new CapturingConnection(response);
            var service = new ZohoCrmService(conn, new FakeLogger<ZohoCrmService>());

            var req = new UpdateUserRequest
            {
                FirstName = "X",
                LastName = "Y",
                Email = "x@y.test",
                Role = new RoleRef { Id = "r" },
                Profile = new ProfileRef { Id = "p" }
            };

            var id = "user/with special?chars#";
            var (status, content) = await service.UpdateUserAsync(id, req, CancellationToken.None);

            Assert.Equal(200, status);
            Assert.Equal("{"ok":true}", content);

            Assert.Equal(HttpMethod.Put, conn.LastMethod);
            Assert.Equal($"/crm/v2/users/{Uri.EscapeDataString(id)}", conn.LastRelativePath);
            Assert.NotNull(conn.LastBody);

            using var doc = JsonDocument.Parse(conn.LastBody!);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("users", out var users));
            var first = users[0];
            Assert.Equal("X", first.GetProperty("first_name").GetString());
            Assert.Equal("Y", first.GetProperty("last_name").GetString());
            Assert.Equal("x@y.test", first.GetProperty("email").GetString());
            Assert.Equal("r", first.GetProperty("role").GetString());
            Assert.Equal("p", first.GetProperty("profile").GetString());
        }
    }

    // Reuse the same FakeLogger implementation for service tests
    internal class FakeLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
