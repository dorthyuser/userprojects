using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;
using zoho_project_csharp.Controllers;
using zoho_project_csharp.Models;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Tests
{
    public class UsersControllerTests
    {
        private class FakeService : IZohoCrmService
        {
            public Task<(int StatusCode, string Content)> GetUsersAsync(CancellationToken cancellationToken) => Task.FromResult((200, "[{"id":"u1"}]"));
            public Task<(int StatusCode, string Content)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken) => Task.FromResult((201, "{"id":"u2"}"));
            public Task<(int StatusCode, string Content)> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken) => Task.FromResult((200, "{"updated":true}"));
        }

        private class NoopLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter) { }
            private class NullScope : System.IDisposable { public static NullScope Instance { get; } = new NullScope(); public void Dispose() { } }
        }

        [Fact]
        public async Task GetUsers_ReturnsContentResult_OnSuccess()
        {
            // Arrange
            var svc = new FakeService();
            var ctrl = new UsersController(svc, new NoopLogger<UsersController>());

            // Act
            var result = await ctrl.GetUsers(CancellationToken.None);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(200, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Contains("u1", contentResult.Content);
        }

        [Fact]
        public async Task CreateUser_ReturnsBadRequest_WhenMissingRequiredFields()
        {
            // Arrange
            var svc = new FakeService();
            var ctrl = new UsersController(svc, new NoopLogger<UsersController>());

            var invalid = new CreateUserRequest(); // missing fields

            // Act
            var result = await ctrl.CreateUser(invalid, CancellationToken.None);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, bad.StatusCode);
            Assert.Contains("required", bad.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenIdEmpty_OrRequestNull_OrInvalidRoleProfile()
        {
            // Arrange
            var svc = new FakeService();
            var ctrl = new UsersController(svc, new NoopLogger<UsersController>());

            // Empty id
            var res1 = await ctrl.UpdateUser("", new UpdateUserRequest(), CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(res1);

            // Null request
            var res2 = await ctrl.UpdateUser("id1", null!, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(res2);

            // role provided but id empty
            var reqWithEmptyRole = new UpdateUserRequest { Role = new RoleRef { Id = "" } };
            var res3 = await ctrl.UpdateUser("id2", reqWithEmptyRole, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(res3);

            // profile provided but id empty
            var reqWithEmptyProfile = new UpdateUserRequest { Profile = new ProfileRef { Id = "" } };
            var res4 = await ctrl.UpdateUser("id3", reqWithEmptyProfile, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(res4);
        }
    }
}
