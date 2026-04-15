using System.Collections.Generic;
using System.Threading.Tasks;
using hello_http_test.Models;

namespace hello_http_test.Services
{
    /// <summary>
    /// Store service contract that abstracts Zoho operations.
    /// </summary>
    public interface IStoreService
    {
        Task<object> GetStoresAsync();
        Task<object> CreateStoreAsync(StoreDto store);
        Task<object> UpdateStoreAsync(string id, StoreDto store);
    }
}
