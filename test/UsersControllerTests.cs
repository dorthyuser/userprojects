using System;
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
            public Func<CancellationToken, Task<(int, string)>> GetUsersImpl { get; set; } = ct => Task.FromResult<(int, string)>((200, "{}"));
            public Func<CreateUserRequest, CancellationToken, Task<(int, string)>> CreateUserImpl { get; set; } = (r, ct) => Task.FromResult<(int, string)>((201, "{}"));
            public Func<string, UpdateUserRequest, CancellationToken, Task<(int, string)>> UpdateUserImpl { get; set; } = (id, r, ct) => Task.FromResult<(int, string)>((200, "{}"));

            public Task<(int StatusCode, string Content)> GetUsersAsync(CancellationToken cancellationToken) => GetUsersImpl(cancellationToken);
            public Task<(int StatusCode, string Content)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken) => CreateUserImpl(request, cancellationToken);
            public Task<(int StatusCode, string Content)> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken) => UpdateUserImpl(id, request, cancellationToken);
        }

        private class NoOpLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
            private class NullScope : IDisposable { public static NullScope Instance { get; } = new NullScope(); public void Dispose() { } }
        }

        [Fact]
        public async Task GetUsers_ReturnsContentResult_OnSuccess()
        {
            var fake = new FakeService();
            fake.GetUsersImpl = ct => Task.FromResult<(int, string)>((200, "{"users":[]}"));
            var controller = new UsersController(fake, new NoOpLogger<UsersController>());

            var result = await controller.GetUsers(CancellationToken.None);

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(200, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal("{"users":[]}", contentResult.Content);
        }

        [Fact]
        public async Task GetUsers_OnException_Returns500()
        {
            var fake = new FakeService();
            fake.GetUsersImpl = ct => throw new InvalidOperationException("boom");
            var controller = new UsersController(fake, new NoOpLogger<UsersController>());

            var result = await controller.GetUsers(CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
            Assert.NotNull(obj.Value);
        }

        [Fact]
        public async Task CreateUser_ReturnsBadRequest_WhenMissingFields()
        {
            var fake = new FakeService();
            var controller = new UsersController(fake, new NoOpLogger<UsersController>());

            var request = new CreateUserRequest { FirstName = "A", LastName = null, Email = "a@example.com", Role = new RoleRef { Id = "" }, Profile = null };

            var result = await controller.CreateUser(request, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task CreateUser_CallsService_AndReturnsContent()
        {
            var fake = new FakeService();
            fake.CreateUserImpl = (req, ct) => Task.FromResult<(int, string)>((201, "{"id":"u1"}"));
            var controller = new UsersController(fake, new NoOpLogger<UsersController>());

            var request = new CreateUserRequest { FirstName = "A", LastName = "B", Email = "a@b.com", Role = new RoleRef { Id = "r1" }, Profile = new ProfileRef { Id = "p1" } };

            var result = await controller.CreateUser(request, CancellationToken.None);

            var content = Assert.IsType<ContentResult>(result);
            Assert.Equal(201, content.StatusCode);
            Assert.Equal("{"id":"u1"}", content.Content);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenIdMissing()
        {
            var fake = new FakeService();
            var controller = new UsersController(fake, new NoOpLogger<UsersController>());

            var result = await controller.UpdateUser("", new UpdateUserRequest(), CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenRequestNull()
        {
            var fake = new FakeService();
            var controller = new UsersController(fake, new NoOpLogger<UsersController>());

            var result = await controller.UpdateUser("id1", null!, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenRoleIdEmpty()
        {
            var fake = new FakeService();
            var controller = new UsersController(fake, new NoOpLogger<UsersController>());

            var req = new UpdateUserRequest { Role = new RoleRef { Id = "" } };
            var result = await controller.UpdateUser("id1", req, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task UpdateUser_CallsService_AndReturnsContent()
        {
            var fake = new FakeService();
            fake.UpdateUserImpl = (id, r, ct) => Task.FromResult<(int, string)>((200, "{"ok":true}"));
            var controller = new UsersController(fake, new NoOpLogger<UsersController>());

            var req = new UpdateUserRequest { FirstName = "X", LastName = "Y" };
            var result = await controller.UpdateUser("id-123", req, CancellationToken.None);

            var content = Assert.IsType<ContentResult>(result);
            Assert.Equal(200, content.StatusCode);
            Assert.Equal("{"ok":true}", content.Content);
        }
    }
}
