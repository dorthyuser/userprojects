using System.Threading;
using System.Threading.Tasks;
using Xunit;
using zoho_project_csharp.Controllers;
using zoho_project_csharp.Services;
using zoho_project_csharp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace zoho_project_csharp.Tests
{
    public class UsersControllerTests
    {
        private class FakeService : IZohoCrmService
        {
            public (int StatusCode, string Content) Response { get; set; } = (200, "{}");

            public Task<(int StatusCode, string Content)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(Response);
            }

            public Task<(int StatusCode, string Content)> GetUsersAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(Response);
            }

            public Task<(int StatusCode, string Content)> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(Response);
            }
        }

        // Minimal no-op logger
        private class NoOpLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter) { }

            private class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new NullScope();
                public void Dispose() { }
            }
        }

        [Fact]
        public async Task GetUsers_ReturnsContentResult_FromService()
        {
            var svc = new FakeService { Response = (200, "[{}]") };
            var controller = new UsersController(svc, new NoOpLogger<UsersController>());

            var result = await controller.GetUsers(CancellationToken.None);

            var content = Assert.IsType<ContentResult>(result);
            Assert.Equal(200, content.StatusCode);
            Assert.Equal("application/json", content.ContentType);
            Assert.Equal("[{}]", content.Content);
        }

        [Fact]
        public async Task CreateUser_ReturnsBadRequest_WhenMissingFields()
        {
            var svc = new FakeService();
            var controller = new UsersController(svc, new NoOpLogger<UsersController>());

            var req = new CreateUserRequest { FirstName = "A" }; // missing last_name, email, role.id, profile.id

            var result = await controller.CreateUser(req, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task CreateUser_ReturnsContentResult_OnSuccess()
        {
            var svc = new FakeService { Response = (201, "{"created":true}") };
            var controller = new UsersController(svc, new NoOpLogger<UsersController>());

            var req = new CreateUserRequest
            {
                FirstName = "A",
                LastName = "B",
                Email = "a@b.com",
                Role = new RoleRef { Id = "r" },
                Profile = new ProfileRef { Id = "p" }
            };

            var result = await controller.CreateUser(req, CancellationToken.None);

            var content = Assert.IsType<ContentResult>(result);
            Assert.Equal(201, content.StatusCode);
            Assert.Equal("application/json", content.ContentType);
            Assert.Equal("{"created":true}", content.Content);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenIdMissing()
        {
            var svc = new FakeService();
            var controller = new UsersController(svc, new NoOpLogger<UsersController>());

            var result = await controller.UpdateUser("", new UpdateUserRequest(), CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenRequestNull()
        {
            var svc = new FakeService();
            var controller = new UsersController(svc, new NoOpLogger<UsersController>());

            var result = await controller.UpdateUser("123", null, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenRoleIdEmpty()
        {
            var svc = new FakeService();
            var controller = new UsersController(svc, new NoOpLogger<UsersController>());

            var req = new UpdateUserRequest { Role = new RoleRef { Id = "" } };

            var result = await controller.UpdateUser("123", req, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task UpdateUser_ReturnsContentResult_OnSuccess()
        {
            var svc = new FakeService { Response = (200, "{"ok":true}") };
            var controller = new UsersController(svc, new NoOpLogger<UsersController>());

            var req = new UpdateUserRequest { FirstName = "X" };

            var result = await controller.UpdateUser("123", req, CancellationToken.None);

            var content = Assert.IsType<ContentResult>(result);
            Assert.Equal(200, content.StatusCode);
            Assert.Equal("application/json", content.ContentType);
            Assert.Equal("{"ok":true}", content.Content);
        }
    }
}
