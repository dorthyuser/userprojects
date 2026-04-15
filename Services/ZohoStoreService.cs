using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using hello_http_test.Models;

namespace hello_http_test.Services
{
    /// <summary>
    /// Application service that provides higher level operations for stores by delegating to the HTTP client wrapper.
    /// </summary>
    public interface IZohoStoreService
    {
        Task<string> GetStoresAsync();
        Task<string> CreateStoreAsync(Store store);
        Task<string> UpdateStoreAsync(string id, Store store);
    }

    public class ZohoStoreService : IZohoStoreService
    {
        private readonly ZohoTestHttpConClient _client;
        private readonly ILogger<ZohoStoreService> _logger;

        public ZohoStoreService(ZohoTestHttpConClient client, ILogger<ZohoStoreService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetStoresAsync()
        {
            return await _client.GetStoresAsync();
        }

        public async Task<string> CreateStoreAsync(Store store)
        {
            return await _client.CreateStoreAsync(store);
        }

        public async Task<string> UpdateStoreAsync(string id, Store store)
        {
            return await _client.UpdateStoreAsync(id, store);
        }
    }
}
