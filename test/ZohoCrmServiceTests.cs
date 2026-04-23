using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Xunit;
using zoho_project_csharp.Services;
using zoho_project_csharp.Models;
using System.Net;

namespace zoho_project_csharp.Tests
{
    public class ZohoCrmServiceTests
    {
        private class FakeConnection : IZohoCrmConnection
        {
            public HttpMethod? LastMethod { get; private set; }
            public string? LastPath { get; private set; }
            public string? LastBody { get; private set; }

            private readonly HttpResponseMessage _response;

            public FakeConnection(HttpStatusCode statusCode, string content)
            {
                _response = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                };
            }

            public Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, string? body, CancellationToken cancellationToken)
            {
                LastMethod = method;
                LastPath = relativePath;
                LastBody = body;
                // Return a clone to simulate real HttpClient behavior
                var clone = new HttpResponseMessage(_response.StatusCode)
                {
                    Content = new StringContent(_response.Content.ReadAsStringAsync().Result, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(clone);
            }
        }

        [Fact]
        public async Task GetUsersAsync_CallsConnectionWithGetAndPath()
        {
            var fake = new FakeConnection(HttpStatusCode.OK, "[{"id":"u1"}]");
            var svc = new ZohoCrmService(fake, Microsoft.Extensions.Logging.Abstractions.NullLogger<ZohoCrmService>.Instance);

            var (status, content) = await svc.GetUsersAsync(CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, status);
            Assert.Equal("[{"id":"u1"}]", content);
            Assert.Equal(HttpMethod.Get, fake.LastMethod);
            Assert.Equal("/crm/v2/users", fake.LastPath);
            Assert.Null(fake.LastBody);
        }

        [Fact]
        public async Task CreateUserAsync_SerializesPayloadWithFlattenedIds()
        {
            var fake = new FakeConnection(HttpStatusCode.Created, "{"created":true}");
            var svc = new ZohoCrmService(fake, Microsoft.Extensions.Logging.Abstractions.NullLogger<ZohoCrmService>.Instance);

            var req = new CreateUserRequest
            {
                FirstName = "Alpha",
                LastName = "Beta",
                Email = "alpha@example.com",
                Role = new RoleRef { Id = "role-123" },
                Profile = new ProfileRef { Id = "profile-123" }
            };

            var (status, content) = await svc.CreateUserAsync(req, CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.Created, status);
            Assert.Equal("{"created":true}", content);

            Assert.Equal(HttpMethod.Post, fake.LastMethod);
            Assert.Equal("/crm/v2/users", fake.LastPath);
            Assert.NotNull(fake.LastBody);
            Assert.Contains(""users":", fake.LastBody);
            Assert.Contains(""first_name":"Alpha"", fake.LastBody);
            Assert.Contains(""role":"role-123"", fake.LastBody);
            Assert.Contains(""profile":"profile-123"", fake.LastBody);
        }

        [Fact]
        public async Task UpdateUserAsync_UsesEscapedIdAndPut()
        {
            var fake = new FakeConnection(HttpStatusCode.OK, "{"updated":true}");
            var svc = new ZohoCrmService(fake, Microsoft.Extensions.Logging.Abstractions.NullLogger<ZohoCrmService>.Instance);

            var req = new UpdateUserRequest
            {
                FirstName = "Nu",
                LastName = "Mu",
                Role = new RoleRef { Id = "r-1" },
                Profile = new ProfileRef { Id = "p-1" }
            };

            var id = "id with spaces/and?chars";
            var expectedPath = "/crm/v2/users/" + System.Uri.EscapeDataString(id);

            var (status, content) = await svc.UpdateUserAsync(id, req, CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, status);
            Assert.Equal("{"updated":true}", content);

            Assert.Equal(HttpMethod.Put, fake.LastMethod);
            Assert.Equal(expectedPath, fake.LastPath);
            Assert.NotNull(fake.LastBody);
            Assert.Contains(""first_name":"Nu"", fake.LastBody);
        }
    }
}
