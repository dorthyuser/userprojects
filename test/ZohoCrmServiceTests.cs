using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Xunit;
using zoho_project_csharp.Services;
using zoho_project_csharp.Models;

namespace zoho_project_csharp.Tests
{
    // Simple fake connection capturing the last call
    internal class FakeConnection : IZohoCrmConnection
    {
        public HttpMethod? LastMethod { get; private set; }
        public string? LastPath { get; private set; }
        public string? LastBody { get; private set; }
        private readonly HttpResponseMessage _response;

        public FakeConnection(HttpResponseMessage response)
        {
            _response = response;
        }

        public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
        {
            LastMethod = method;
            LastPath = relativePath;
            LastBody = body;
            // Return a clone to mimic real behavior
            var clone = new HttpResponseMessage(_response.StatusCode)
            {
                Content = _response.Content is null ? null : new StringContent(_response.Content.ReadAsStringAsync(cancellationToken).Result, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(clone);
        }
    }

    public class ZohoCrmServiceTests
    {
        [Fact]
        public async Task CreateUserAsync_SerializesAndForwardsRoleProfileAsStrings()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{"result":"ok"}", Encoding.UTF8, "application/json")
            };

            var fakeConn = new FakeConnection(response);
            var service = new ZohoCrmService(fakeConn, new FakeLogger<ZohoCrmService>());

            var req = new CreateUserRequest
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Role = new RoleRef { Id = "role-id" },
                Profile = new ProfileRef { Id = "profile-id" }
            };

            // Act
            var (status, content) = await service.CreateUserAsync(req, CancellationToken.None);

            // Assert
            Assert.Equal((int)HttpStatusCode.Created, status);
            Assert.Equal("{"result":"ok"}", content);

            Assert.Equal(HttpMethod.Post, fakeConn.LastMethod);
            Assert.Equal("/crm/v2/users", fakeConn.LastPath);
            Assert.NotNull(fakeConn.LastBody);
            Assert.Contains(""role":"role-id"", fakeConn.LastBody!);
            Assert.Contains(""profile":"profile-id"", fakeConn.LastBody!);
            Assert.Contains(""first_name":"John"", fakeConn.LastBody!);
        }

        [Fact]
        public async Task UpdateUserAsync_EncodesIdInPath_AndUsesPut()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{"updated":true}", Encoding.UTF8, "application/json")
            };

            var fakeConn = new FakeConnection(response);
            var service = new ZohoCrmService(fakeConn, new FakeLogger<ZohoCrmService>());

            var request = new UpdateUserRequest
            {
                FirstName = "Jane",
                Email = "jane@example.com",
                Role = new RoleRef { Id = "r" }
            };

            var id = "user/id"; // contains slash to ensure escaping

            // Act
            var (status, content) = await service.UpdateUserAsync(id, request, CancellationToken.None);

            // Assert
            Assert.Equal((int)HttpStatusCode.OK, status);
            Assert.Equal("{"updated":true}", content);
            Assert.Equal(HttpMethod.Put, fakeConn.LastMethod);
            Assert.Equal($"/crm/v2/users/{Uri.EscapeDataString(id)}", fakeConn.LastPath);
            Assert.Contains(""first_name":"Jane"", fakeConn.LastBody!);
            Assert.Contains(""role":"r"", fakeConn.LastBody!);
        }
    }

    // Minimal logger used in service tests
    internal class FakeLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
