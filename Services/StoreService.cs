using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using hello_http_test.Models;

namespace hello_http_test.Services
{
    /// <summary>
    /// Implements store-level operations by delegating to the Zoho HTTP client.
    /// </summary>
    public class StoreService : IStoreService
    {
        private readonly ZohoTestHttpConClient _zohoClient;
        private readonly ILogger<StoreService> _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        public StoreService(ZohoTestHttpConClient zohoClient, ILogger<StoreService> logger)
        {
            _zohoClient = zohoClient ?? throw new ArgumentNullException(nameof(zohoClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<object> GetStoresAsync()
        {
            return await _zohoClient.GetStoresAsync();
        }

        /// <inheritdoc />
        public async Task<object> CreateStoreAsync(StoreDto store)
        {
            return await _zohoClient.CreateStoreAsync(store);
        }

        /// <inheritdoc />
        public async Task<object> UpdateStoreAsync(string id, StoreDto store)
        {
            return await _zohoClient.UpdateStoreAsync(id, store);
        }
    }
}
