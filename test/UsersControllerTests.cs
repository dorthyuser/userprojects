using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;
using zoho_project_csharp.Controllers;
using zoho_project_csharp.Models;
using zoho_project_csharp.Services;

namespace zoho_project_csharp.Tests
{
    public class UsersControllerTests
    {
        private class FakeZohoService : IZohoCrmService
        {
            public (int StatusCode, string Content) NextGetUsersResult { get; set; } = (200, "[]");
            public (int StatusCode, string Content) NextCreateResult { get; set; } = (201, "{}");
            public (int StatusCode, string Content) NextUpdateResult { get; set; } = (200, "{}");

            public HttpMethod? LastMethod { get; private set; }
            public string? LastPath { get; private set; }
            public string? LastBody { get; private set; }

            public Task<(int StatusCode, string Content)> GetUsersAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(NextGetUsersResult);
            }

            public Task<(int StatusCode, string Content)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(NextCreateResult);
            }

            public Task<(int StatusCode, string Content)> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(NextUpdateResult);
            }
        }

        [Fact]
        public async Task GetUsers_ReturnsContentResultFromService()
        {
            var fake = new FakeZohoService { NextGetUsersResult = (200, "[{"id":"u1"}]") };
            var controller = new UsersController(fake, NullLogger<UsersController>.Instance);

            var result = await controller.GetUsers(CancellationToken.None);

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(200, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal("[{"id":"u1"}]", contentResult.Content);
        }

        [Fact]
        public async Task CreateUser_MissingFields_ReturnsBadRequest()
        {
            var fake = new FakeZohoService();
            var controller = new UsersController(fake, NullLogger<UsersController>.Instance);

            var req = new CreateUserRequest
            {
                FirstName = "",
                LastName = null,
                Email = "",
                Role = null,
                Profile = null
            };

            var result = await controller.CreateUser(req, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var json = JsonSerializer.Serialize(bad.Value);
            Assert.Contains("first_name", json);
        }

        [Fact]
        public async Task CreateUser_Valid_CallsServiceAndReturnsContent()
        {
            var fake = new FakeZohoService { NextCreateResult = (201, "{"ok":true}") };
            var controller = new UsersController(fake, NullLogger<UsersController>.Instance);

            var req = new CreateUserRequest
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Role = new RoleRef { Id = "role-1" },
                Profile = new ProfileRef { Id = "profile-1" }
            };

            var result = await controller.CreateUser(req, CancellationToken.None);

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(201, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal("{"ok":true}", contentResult.Content);
        }

        [Fact]
        public async Task UpdateUser_IdMissing_ReturnsBadRequest()
        {
            var fake = new FakeZohoService();
            var controller = new UsersController(fake, NullLogger<UsersController>.Instance);

            var result = await controller.UpdateUser("", new UpdateUserRequest(), CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var json = JsonSerializer.Serialize(bad.Value);
            Assert.Contains("id is required", json);
        }

        [Fact]
        public async Task UpdateUser_RequestNull_ReturnsBadRequest()
        {
            var fake = new FakeZohoService();
            var controller = new UsersController(fake, NullLogger<UsersController>.Instance);

            var result = await controller.UpdateUser("123", null, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var json = JsonSerializer.Serialize(bad.Value);
            Assert.Contains("request body is required", json);
        }

        [Fact]
        public async Task UpdateUser_RoleProvidedEmptyId_ReturnsBadRequest()
        {
            var fake = new FakeZohoService();
            var controller = new UsersController(fake, NullLogger<UsersController>.Instance);

            var req = new UpdateUserRequest
            {
                Role = new RoleRef { Id = "" }
            };

            var result = await controller.UpdateUser("123", req, CancellationToken.None);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var json = JsonSerializer.Serialize(bad.Value);
            Assert.Contains("role.id", json);
        }

        [Fact]
        public async Task UpdateUser_Valid_CallsServiceAndReturnsContent()
        {
            var fake = new FakeZohoService { NextUpdateResult = (204, "{"updated":true}") };
            var controller = new UsersController(fake, NullLogger<UsersController>.Instance);

            var req = new UpdateUserRequest
            {
                FirstName = "Jane",
                LastName = "Roe",
                Email = "jane@example.com",
                Role = new RoleRef { Id = "role-2" },
                Profile = new ProfileRef { Id = "profile-2" }
            };

            var result = await controller.UpdateUser("abc-123", req, CancellationToken.None);

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(204, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal("{"updated":true}", contentResult.Content);
        }
    }
}
