using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Xunit;
using zoho_project_csharp.Controllers;
using zoho_project_csharp.Models;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Tests
{
    // Minimal no-op logger to avoid external dependencies
    internal class FakeLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    internal class FakeZohoService : IZohoCrmService
    {
        public Func<CancellationToken, Task<(int, string)>>? GetUsersImpl { get; set; }
        public Func<CreateUserRequest, CancellationToken, Task<(int, string)>>? CreateUserImpl { get; set; }
        public Func<string, UpdateUserRequest, CancellationToken, Task<(int, string)>>? UpdateUserImpl { get; set; }

        public Task<(int StatusCode, string Content)> GetUsersAsync(CancellationToken cancellationToken)
        {
            if (GetUsersImpl != null)
                return GetUsersImpl(cancellationToken);
            throw new NotImplementedException();
        }

        public Task<(int StatusCode, string Content)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
        {
            if (CreateUserImpl != null)
                return CreateUserImpl(request, cancellationToken);
            throw new NotImplementedException();
        }

        public Task<(int StatusCode, string Content)> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            if (UpdateUserImpl != null)
                return UpdateUserImpl(id, request, cancellationToken);
            throw new NotImplementedException();
        }
    }

    public class UsersControllerTests
    {
        [Fact]
        public async Task GetUsers_Returns_ContentResult_OnSuccess()
        {
            var fake = new FakeZohoService
            {
                GetUsersImpl = ct => Task.FromResult((200, "{"ok":true}" ))
            };

            var controller = new UsersController(fake, new FakeLogger<UsersController>());

            var result = await controller.GetUsers(CancellationToken.None);

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(200, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal("{"ok":true}", contentResult.Content);
        }

        [Fact]
        public async Task GetUsers_Returns_500_OnException()
        {
            var fake = new FakeZohoService
            {
                GetUsersImpl = ct => throw new InvalidOperationException("boom")
            };

            var controller = new UsersController(fake, new FakeLogger<UsersController>());

            var result = await controller.GetUsers(CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);

            var val = obj.Value ?? throw new Exception("no value");
            var prop = val.GetType().GetProperty("error");
            Assert.NotNull(prop);
            Assert.Equal("boom", prop!.GetValue(val)?.ToString());
        }

        [Fact]
        public async Task CreateUser_Returns_BadRequest_When_Missing_Fields()
        {
            var fake = new FakeZohoService();
            var controller = new UsersController(fake, new FakeLogger<UsersController>());

            var req = new CreateUserRequest { FirstName = "John" }; // missing last_name, email, role, profile

            var result = await controller.CreateUser(req, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var val = bad.Value ?? throw new Exception("no value");
            var prop = val.GetType().GetProperty("error");
            Assert.NotNull(prop);
            Assert.Equal("first_name, last_name, email, role.id and profile.id are required", prop!.GetValue(val)?.ToString());
        }

        [Fact]
        public async Task CreateUser_Calls_Service_And_Returns_ContentResult()
        {
            var fake = new FakeZohoService
            {
                CreateUserImpl = (r, ct) => Task.FromResult((201, "{"created":true}"))
            };

            var controller = new UsersController(fake, new FakeLogger<UsersController>());

            var req = new CreateUserRequest
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Role = new RoleRef { Id = "role1" },
                Profile = new ProfileRef { Id = "profile1" }
            };

            var result = await controller.CreateUser(req, CancellationToken.None);

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(201, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal("{"created":true}", contentResult.Content);
        }

        [Fact]
        public async Task UpdateUser_Returns_BadRequest_When_Id_Missing()
        {
            var fake = new FakeZohoService();
            var controller = new UsersController(fake, new FakeLogger<UsersController>());

            var result = await controller.UpdateUser("", new UpdateUserRequest(), CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var val = bad.Value ?? throw new Exception("no value");
            var prop = val.GetType().GetProperty("error");
            Assert.NotNull(prop);
            Assert.Equal("id is required", prop!.GetValue(val)?.ToString());
        }

        [Fact]
        public async Task UpdateUser_Returns_BadRequest_When_Request_Null()
        {
            var fake = new FakeZohoService();
            var controller = new UsersController(fake, new FakeLogger<UsersController>());

            var result = await controller.UpdateUser("123", null!, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var val = bad.Value ?? throw new Exception("no value");
            var prop = val.GetType().GetProperty("error");
            Assert.NotNull(prop);
            Assert.Equal("request body is required", prop!.GetValue(val)?.ToString());
        }

        [Fact]
        public async Task UpdateUser_Returns_BadRequest_When_RoleId_Empty()
        {
            var fake = new FakeZohoService();
            var controller = new UsersController(fake, new FakeLogger<UsersController>());

            var req = new UpdateUserRequest { Role = new RoleRef { Id = "" } };
            var result = await controller.UpdateUser("123", req, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var val = bad.Value ?? throw new Exception("no value");
            var prop = val.GetType().GetProperty("error");
            Assert.NotNull(prop);
            Assert.Equal("role.id, if provided, must be non-empty", prop!.GetValue(val)?.ToString());
        }

        [Fact]
        public async Task UpdateUser_Calls_Service_And_Returns_ContentResult()
        {
            var fake = new FakeZohoService
            {
                UpdateUserImpl = (id, r, ct) => Task.FromResult((200, "{"updated":true}"))
            };

            var controller = new UsersController(fake, new FakeLogger<UsersController>());

            var req = new UpdateUserRequest
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com",
                Role = new RoleRef { Id = "role2" },
                Profile = new ProfileRef { Id = "profile2" }
            };

            var result = await controller.UpdateUser("abc-123", req, CancellationToken.None);

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(200, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal("{"updated":true}", contentResult.Content);
        }
    }
}
