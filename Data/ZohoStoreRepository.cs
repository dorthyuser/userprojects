using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using hello_http_test.Services;

namespace hello_http_test.Data
{
    /// <summary>
    /// Repository implementation that delegates to ZohoTestHttpConClient via the service layer.
    /// </summary>
    public class ZohoStoreRepository : IZohoStoreRepository
    {
        private readonly IZohoStoreService _service;
        private readonly ILogger<ZohoStoreRepository> _logger;

        public ZohoStoreRepository(IZohoStoreService service, ILogger<ZohoStoreRepository> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetStoresAsync()
        {
            return await _service.GetStoresAsync();
        }

        public async Task<string> CreateStoreAsync(object store)
        {
            // object is used to avoid forcing shape; service will serialize
            return await _service.CreateStoreAsync((hello_http_test.Models.Store)store);
        }

        public async Task<string> UpdateStoreAsync(string id, object store)
        {
            return await _service.UpdateStoreAsync(id, (hello_http_test.Models.Store)store);
        }
    }
}
