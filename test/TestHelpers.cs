using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using hello_http_test.Services;
using hello_http_test.Models;

namespace hello_http_test.Tests
{
    // Simple fake logger implementing ILogger<T>
    public class NullLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }

    // Simple HttpMessageHandler allowing to return configured responses per request
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder ?? throw new ArgumentNullException(nameof(responder));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    // Simple IHttpClientFactory returning named clients from a map
    public class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpClient> _clients = new Dictionary<string, HttpClient>();

        public void AddClient(string name, HttpClient client)
        {
            _clients[name] = client;
        }

        public HttpClient CreateClient(string name)
        {
            if (name != null && _clients.TryGetValue(name, out var c)) return c;
            return new HttpClient();
        }
    }

    // Lightweight fake IStoreService for controller tests
    public class FakeStoreService : hello_http_test.Services.IStoreService
    {
        public Func<Task<object>> OnGet { get; set; }
        public Func<hello_http_test.Models.StoreDto, Task<object>> OnCreate { get; set; }
        public Func<string, hello_http_test.Models.StoreDto, Task<object>> OnUpdate { get; set; }

        public Task<object> GetStoresAsync() => OnGet != null ? OnGet() : Task.FromResult<object>(new { });
        public Task<object> CreateStoreAsync(hello_http_test.Models.StoreDto store) => OnCreate != null ? OnCreate(store) : Task.FromResult<object>(new { });
        public Task<object> UpdateStoreAsync(string id, hello_http_test.Models.StoreDto store) => OnUpdate != null ? OnUpdate(id, store) : Task.FromResult<object>(new { });
    }

    // Lightweight fake IZohoStoreService for StoresController tests
    public class FakeZohoStoreService : hello_http_test.Services.IZohoStoreService
    {
        public Func<Task<string>> OnGet { get; set; }
        public Func<hello_http_test.Models.Store, Task<string>> OnCreate { get; set; }
        public Func<string, hello_http_test.Models.Store, Task<string>> OnUpdate { get; set; }

        public Task<string> GetStoresAsync() => OnGet != null ? OnGet() : Task.FromResult<string>(string.Empty);
        public Task<string> CreateStoreAsync(hello_http_test.Models.Store store) => OnCreate != null ? OnCreate(store) : Task.FromResult<string>(string.Empty);
        public Task<string> UpdateStoreAsync(string id, hello_http_test.Models.Store store) => OnUpdate != null ? OnUpdate(id, store) : Task.FromResult<string>(string.Empty);
    }

    // Minimal IConfiguration wrapper util for tests
    public static class TestConfig
    {
        public static IConfigurationRoot Build(Dictionary<string, string> values) => new ConfigurationBuilder().AddInMemoryCollection(values ?? new Dictionary<string, string>()).Build();
    }

    // Fake ZohoAuthService alternative for testing TokenHandler is not provided because TokenHandler
    // depends on concrete ZohoAuthService with non-virtual methods. We instead test TokenService and
    // ZohoAuthService behavior via HttpClient message handlers.
}
