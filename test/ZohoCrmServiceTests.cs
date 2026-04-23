using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Xunit;
using zoho_project_csharp.Services;
using zoho_project_csharp.Models;

public class ZohoCrmServiceTests
{
    private class FakeConnection : IZohoCrmConnection
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
            // Return a clone so disposal by caller won't affect other assertions
            var copy = new HttpResponseMessage(_response.StatusCode)
            {
                Content = new StringContent(_response.Content.ReadAsStringAsync(cancellationToken).Result, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(copy);
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
    public async Task CreateUserAsync_FlattensRoleAndProfileAndSendsPayload()
    {
        // Arrange
        var apiResponse = new HttpResponseMessage(System.Net.HttpStatusCode.Created)
        {
            Content = new StringContent("{"result":"ok"}", Encoding.UTF8, "application/json")
        };

        var fake = new FakeConnection(apiResponse);
        var service = new ZohoCrmService(fake, new TestLogger<ZohoCrmService>());

        var request = new CreateUserRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Role = new RoleRef { Id = "role-id" },
            Profile = new ProfileRef { Id = "profile-id" }
        };

        // Act
        var (status, content) = await service.CreateUserAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(201, status);
        Assert.Equal("{"result":"ok"}", content);
        Assert.Equal(HttpMethod.Post, fake.LastMethod);
        Assert.Equal("/crm/v2/users", fake.LastPath);
        Assert.NotNull(fake.LastBody);
        // Basic checks that flattened ids and fields are present
        Assert.Contains(""first_name":"John"", fake.LastBody!);
        Assert.Contains(""last_name":"Doe"", fake.LastBody!);
        Assert.Contains(""email":"john.doe@example.com"", fake.LastBody!);
        Assert.Contains(""role":"role-id"", fake.LastBody!);
        Assert.Contains(""profile":"profile-id"", fake.LastBody!);
        // Wrapper check
        Assert.Contains(""users":", fake.LastBody!);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsStatusAndContentFromConnection()
    {
        // Arrange
        var body = "[{"id":"u1","email":"a@b.com"}]";
        var apiResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        var fake = new FakeConnection(apiResponse);
        var service = new ZohoCrmService(fake, new TestLogger<ZohoCrmService>());

        // Act
        var (status, content) = await service.GetUsersAsync(CancellationToken.None);

        // Assert
        Assert.Equal(200, status);
        Assert.Equal(body, content);
        Assert.Equal(HttpMethod.Get, fake.LastMethod);
        Assert.Equal("/crm/v2/users", fake.LastPath);
    }
}
