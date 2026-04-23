using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using zoho_project_csharp.Controllers;
using zoho_project_csharp.Models;
using zoho_project_csharp.Services;

public class UsersControllerTests
{
    private class FakeService : IZohoCrmService
    {
        public string? LastCreateBody { get; private set; }
        public (int StatusCode, string Content) GetUsersResult { get; set; } = (200, "[]");
        public (int StatusCode, string Content) CreateUserResult { get; set; } = (201, "{"id":"u1"}");
        public (int StatusCode, string Content) UpdateUserResult { get; set; } = (200, "{"ok":true}");

        public Task<(int StatusCode, string Content)> GetUsersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(GetUsersResult);
        }

        public Task<(int StatusCode, string Content)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
        {
            // Serialize minimal representation to capture flattening if needed by tests
            LastCreateBody = System.Text.Json.JsonSerializer.Serialize(request);
            return Task.FromResult(CreateUserResult);
        }

        public Task<(int StatusCode, string Content)> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateUserResult);
        }
    }

    private class TestLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter) { }

        private class NullScope : System.IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }
    }

    [Fact]
    public async Task GetUsers_ReturnsContentResult_OnSuccess()
    {
        // Arrange
        var fake = new FakeService { GetUsersResult = (200, "[{"id":"u1"}]") };
        var controller = new UsersController(fake, new TestLogger<UsersController>());

        // Act
        var result = await controller.GetUsers(CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, contentResult.StatusCode);
        Assert.Equal("application/json", contentResult.ContentType);
        Assert.Equal("[{"id":"u1"}]", contentResult.Content);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenMissingRequiredFields()
    {
        // Arrange
        var fake = new FakeService();
        var controller = new UsersController(fake, new TestLogger<UsersController>());

        var badRequest = new CreateUserRequest
        {
            FirstName = null,
            LastName = "Doe",
            Email = "",
            Role = new RoleRef { Id = null },
            Profile = null
        };

        // Act
        var result = await controller.CreateUser(badRequest, CancellationToken.None);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
        var str = System.Text.Json.JsonSerializer.Serialize(bad.Value);
        Assert.Contains("first_name, last_name, email, role.id and profile.id are required", str);
    }
}
