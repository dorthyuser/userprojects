using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using zoho_project_csharp.Controllers;
using zoho_project_csharp.Models;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Tests
{
    internal class StubZohoService : IZohoCrmService
    {
        public (int StatusCode, string Content) NextResponse { get; set; } = (200, "{}");

        public Task<(int StatusCode, string Content)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(NextResponse);
        }

        public Task<(int StatusCode, string Content)> GetUsersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(NextResponse);
        }

        public Task<(int StatusCode, string Content)> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(NextResponse);
        }
    }

    internal class FakeLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter) { }
    }

    public class UsersControllerTests
    {
        [Fact]
        public async Task GetUsers_ReturnsContentResult_WithServiceContent()
        {
            var stub = new StubZohoService { NextResponse = (200, "{"users":[]}") };
            var controller = new UsersController(stub, new FakeLogger<UsersController>());

            var result = await controller.GetUsers(CancellationToken.None);

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(200, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal("{"users":[]}", contentResult.Content);
        }

        [Fact]
        public async Task CreateUser_MissingRequiredFields_ReturnsBadRequest()
        {
            var stub = new StubZohoService();
            var controller = new UsersController(stub, new FakeLogger<UsersController>());

            var req = new CreateUserRequest { FirstName = "A" }; // missing last_name, email, role, profile
            var result = await controller.CreateUser(req, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("first_name, last_name, email, role.id and profile.id are required", bad.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task CreateUser_ValidForwardsToService_ReturnsContentResult()
        {
            var stub = new StubZohoService { NextResponse = (201, "{"created":true}") };
            var controller = new UsersController(stub, new FakeLogger<UsersController>());

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
            Assert.Equal("{"created":true}", content.Content);
        }

        [Fact]
        public async Task UpdateUser_MissingId_ReturnsBadRequest()
        {
            var stub = new StubZohoService();
            var controller = new UsersController(stub, new FakeLogger<UsersController>());

            var res = await controller.UpdateUser("", new UpdateUserRequest(), CancellationToken.None);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            Assert.Contains("id is required", bad.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task UpdateUser_NullBody_ReturnsBadRequest()
        {
            var stub = new StubZohoService();
            var controller = new UsersController(stub, new FakeLogger<UsersController>());

            var res = await controller.UpdateUser("123", null, CancellationToken.None);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            Assert.Contains("request body is required", bad.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task UpdateUser_EmptyRoleId_ReturnsBadRequest()
        {
            var stub = new StubZohoService();
            var controller = new UsersController(stub, new FakeLogger<UsersController>());

            var req = new UpdateUserRequest { Role = new RoleRef { Id = "" } };
            var res = await controller.UpdateUser("123", req, CancellationToken.None);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            Assert.Contains("role.id, if provided, must be non-empty", bad.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task UpdateUser_ValidForwardsToService_ReturnsContentResult()
        {
            var stub = new StubZohoService { NextResponse = (200, "{"ok":true}") };
            var controller = new UsersController(stub, new FakeLogger<UsersController>());

            var req = new UpdateUserRequest { FirstName = "X" };
            var res = await controller.UpdateUser("123", req, CancellationToken.None);

            var content = Assert.IsType<ContentResult>(res);
            Assert.Equal(200, content.StatusCode);
            Assert.Equal("{"ok":true}", content.Content);
        }
    }
}
